using Amazon.S3;
using Amazon.S3.Model;
using FFMpegCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NAudio.Wave;
using Podcast_MVC.Data;
using Podcast_MVC.Models;
using Podcast_MVC.Models.ViewModels;
using Podcast_MVC.Services;
using System.Security.Claims;

namespace Podcast_MVC.Controllers
{
    [Authorize(Roles = "Podcaster")]
    public class PodcasterController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly AwsService _awsService;
        public PodcasterController(ApplicationDbContext context, AwsService awsService)
        {
            _context = context;
            _awsService = awsService;
        }

        public IActionResult Dashboard()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var podcasts = _context.Podcasts
                                   .Where(p => p.CreatorID == userId)
                                   .ToList();
            return View("Dashboard", podcasts);
        }


        public IActionResult CreatePodcast()
        {
            return View();
        }

        [HttpPost]
        [HttpPost]
        public IActionResult CreatePodcast(Podcast podcast)
        {
            podcast.CreatorID = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (ModelState.IsValid)
            {
                _context.Podcasts.Add(podcast);
                _context.SaveChanges();
            }

            return RedirectToAction("Dashboard");
        }

        public IActionResult EditPodcast(int id)
        {
            var podcast = _context.Podcasts.Find(id);
            if (podcast == null) 
                return NotFound(); 
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (podcast.CreatorID != userId)
                return Forbid();
            return View(podcast);
        }

        [HttpPost]
        public IActionResult EditPodcast(Podcast podcast)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var existingPodcast = _context.Podcasts.FirstOrDefault(p => p.PodcastID == podcast.PodcastID);
            if (existingPodcast == null)
                return NotFound();

            if (existingPodcast.CreatorID != userId)
                return Forbid();

            existingPodcast.Title = podcast.Title;
            existingPodcast.Description = podcast.Description;

            _context.SaveChanges();

            return RedirectToAction("Dashboard");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeletePodcast(int id)
        {
            var podcast = await _context.Podcasts
                .Include(p => p.Episodes) // include episodes
                .FirstOrDefaultAsync(p => p.PodcastID == id);

            if (podcast == null)
                return NotFound();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (podcast.CreatorID != userId)
                return Forbid();

            var s3 = _awsService.S3Client;
            var bucket = _awsService.BucketName;

            // Collect all S3 keys first
            var keysToDelete = podcast.Episodes
                .Where(e => !string.IsNullOrEmpty(e.AudioFileURL))
                .Select(e => new Amazon.S3.Model.KeyVersion { Key = e.AudioFileURL })
                .ToList();

            if (keysToDelete.Any())
            {
                var deleteRequest = new Amazon.S3.Model.DeleteObjectsRequest
                {
                    BucketName = bucket,
                    Objects = keysToDelete
                };

                try
                {
                    await s3.DeleteObjectsAsync(deleteRequest);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to bulk delete S3 objects: {ex.Message}");
                }
            }

            // Remove episodes from DB first
            _context.Episodes.RemoveRange(podcast.Episodes);

            // Remove the podcast itself
            _context.Podcasts.Remove(podcast);

            await _context.SaveChangesAsync();

            return RedirectToAction("Dashboard");
        }


        public IActionResult ManageEpisodes(int id)
        {
            // Find the podcast
            var podcast = _context.Podcasts
                                  .Include(p => p.Episodes)
                                  .FirstOrDefault(p => p.PodcastID == id);

            if (podcast == null)
                return NotFound();

            // Ensure the logged-in user owns this podcast
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (podcast.CreatorID != userId)
                return Forbid();

            return View(podcast);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddEpisode(int PodcastID, string Title, IFormFile AudioFile)
        {
            var podcast = await _context.Podcasts.FindAsync(PodcastID);
            if (podcast == null)
                return NotFound();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (podcast.CreatorID != userId)
                return Forbid();

            if (string.IsNullOrWhiteSpace(Title))
                return BadRequest("Title is required.");

            // --- MP3-ONLY VALIDATION ---
            if (AudioFile == null || AudioFile.Length == 0)
                return BadRequest("No file uploaded.");
            var isMp3ByMime = string.Equals(AudioFile.ContentType, "audio/mpeg", StringComparison.OrdinalIgnoreCase);
            var isMp3ByExt = Path.GetExtension(AudioFile.FileName).Equals(".mp3", StringComparison.OrdinalIgnoreCase);
            if (!(isMp3ByMime || isMp3ByExt))
                return BadRequest("Only MP3 files are allowed.");

            // Optional size cap (example: 150 MB)
            if (AudioFile.Length > 150L * 1024 * 1024)
                return BadRequest("File too large. Max 150 MB.");

            var s3 = _awsService.S3Client;
            var bucket = _awsService.BucketName;

            double durationMinutes = 0;
            var tempFile = Path.GetTempFileName();

            try
            {
                // Save to temp file
                using (var fileStream = System.IO.File.Create(tempFile))
                    await AudioFile.CopyToAsync(fileStream);

                try
                {
                    using var fs = System.IO.File.OpenRead(tempFile);
                    using var mp3 = new Mp3FileReader(fs); 
                    durationMinutes = mp3.TotalTime.TotalMinutes;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Mp3 duration read failed: {ex.Message}");
                    durationMinutes = 0; 
                }

                using (var uploadStream = System.IO.File.OpenRead(tempFile))
                {
                    var safeName = Path.GetFileName(AudioFile.FileName);
                    var key = $"{PodcastID}/{Guid.NewGuid()}_{safeName}";

                    var putRequest = new PutObjectRequest
                    {
                        BucketName = bucket,
                        Key = key,
                        InputStream = uploadStream,
                        ContentType = "audio/mpeg"
                    };

                    await s3.PutObjectAsync(putRequest);

                    var episode = new Episode
                    {
                        Title = Title.Trim(),
                        AudioFileURL = key,     // S3 key
                        PodcastID = PodcastID,
                        Duration = durationMinutes,
                        Status = EpisodeStatus.Pending
                    };

                    _context.Episodes.Add(episode);
                    await _context.SaveChangesAsync();
                }
            }
            finally
            {
                try { System.IO.File.Delete(tempFile); } catch { /* ignore */ }
            }

            return RedirectToAction("ManageEpisodes", new { id = PodcastID });
        }


        [HttpPost]
        public async Task<IActionResult> DeleteEpisode(int id)
        {
            // Find the episode by ID
            var episode = await _context.Episodes.FindAsync(id);
            if (episode == null)
                return NotFound();

            // Verify the episode belongs to the current user's podcast
            var podcast = await _context.Podcasts.FindAsync(episode.PodcastID);
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (podcast == null || podcast.CreatorID != userId)
                return Forbid();

            // Delete from S3 if it exists
            try
            {
                var s3 = _awsService.S3Client;
                var bucket = _awsService.BucketName;

                // Extract the S3 object key from the stored URL
                var key = episode.AudioFileURL;

                await s3.DeleteObjectAsync(bucket, key);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"S3 deletion failed: {ex.Message}");
                // Don’t stop the process — continue deleting from DB
            }

            // Remove from DB
            _context.Episodes.Remove(episode);
            await _context.SaveChangesAsync();

            // Redirect back to the same ManageEpisodes page
            return RedirectToAction("ManageEpisodes", new { id = episode.PodcastID });
        }

        public async Task<IActionResult> EditEpisode(int id)
        {
            var episode = await _context.Episodes
                .Include(e => e.Podcast)
                .FirstOrDefaultAsync(e => e.EpisodeID == id);

            if (episode == null)
                return NotFound();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (episode.Podcast.CreatorID != userId)
                return Forbid();

            var comments = await _awsService.DbContext
            .QueryAsync<Comment>(episode.EpisodeID)
            .GetRemainingAsync();

            var vm = new EpisodePodcasterViewModel
            {
                Episode = episode,
                Comments = comments.OrderByDescending(c => c.Timestamp).ToList()
            };
            return View(vm);
        }


        [HttpPost]
        public async Task<IActionResult> EditEpisode(int id, string Title, IFormFile? AudioFile)
        {
            var episode = await _context.Episodes
                .Include(e => e.Podcast)
                .FirstOrDefaultAsync(e => e.EpisodeID == id);

            if (episode == null)
                return NotFound();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (episode.Podcast.CreatorID != userId)
                return Forbid();

            // Reset status for another check by admin
            episode.Status = EpisodeStatus.Pending;
            // Update title regardless
            episode.Title = Title;

            var s3 = _awsService.S3Client;
            var bucket = _awsService.BucketName;

            // Only replace audio if a new file was uploaded
            if (AudioFile != null && AudioFile.Length > 0)
            {
                try
                {
                    double durationMinutes = 0;
                    var tempFile = Path.GetTempFileName();

                    // Delete old file only if there is a valid URL
                    if (!string.IsNullOrEmpty(episode.AudioFileURL))
                    {
                        var oldKey = episode.AudioFileURL;
                        await s3.DeleteObjectAsync(bucket, oldKey);
                    }

                    // Save to temp file
                    using (var fileStream = System.IO.File.Create(tempFile))
                        await AudioFile.CopyToAsync(fileStream);

                    try
                    {
                        using var fs = System.IO.File.OpenRead(tempFile);
                        using var mp3 = new Mp3FileReader(fs);
                        durationMinutes = mp3.TotalTime.TotalMinutes;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Mp3 duration read failed: {ex.Message}");
                        durationMinutes = 0;
                    }

                    // Upload the new file
                    using var stream = AudioFile.OpenReadStream();
                    var newKey = $"{episode.PodcastID}/{Guid.NewGuid()}_{AudioFile.FileName}";

                    var putRequest = new PutObjectRequest
                    {
                        BucketName = bucket,
                        Key = newKey,
                        InputStream = stream,
                        ContentType = AudioFile.ContentType
                    };

                    await s3.PutObjectAsync(putRequest);

                    // Update DB with new URL
                    episode.AudioFileURL = newKey;
                    episode.Duration = durationMinutes;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"S3 error during update: {ex.Message}");
                }
            }

            await _context.SaveChangesAsync();

            return RedirectToAction("ManageEpisodes", new { id = episode.PodcastID });
        }


        [HttpGet]
        public async Task<IActionResult> DownloadEpisodeAudio(int id)
        {
            var episode = await _context.Episodes
                .Include(e => e.Podcast)
                .FirstOrDefaultAsync(e => e.EpisodeID == id);

            if (episode == null)
                return NotFound();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (episode.Podcast.CreatorID != userId)
                return Forbid();

            if (string.IsNullOrEmpty(episode.AudioFileURL))
                return NotFound("Audio file not found.");

            // Get object from S3
            try
            {
                Console.WriteLine(episode.AudioFileURL);
                var response = await _awsService.S3Client.GetObjectAsync(_awsService.BucketName, episode.AudioFileURL);

                return File(response.ResponseStream, response.Headers.ContentType, System.IO.Path.GetFileName(episode.AudioFileURL));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"S3 download error: {ex.Message}");
                return StatusCode(500, "Error retrieving audio from S3.");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddComment(int EpisodeID, string Content)
        {
            if (string.IsNullOrWhiteSpace(Content))
                return RedirectToAction("EditEpisode", new { id = EpisodeID });

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var episode = await _context.Episodes
                .Include(e => e.Podcast)
                .FirstOrDefaultAsync(e => e.EpisodeID == EpisodeID);

            if (episode == null)
                return NotFound();

            var db = _awsService.DbContext;

            var comment = new Comment
            {
                EpisodeID = EpisodeID,
                PodcastID = episode.PodcastID,
                UserID = userId,
                Text = Content,
                Timestamp = DateTime.UtcNow,
                UserName = User.Identity.Name ?? "Unknown"
            };

            try
            {
                await db.SaveAsync(comment);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to add comment: {ex.Message}");
                TempData["Error"] = "Failed to add comment.";
            }

            return RedirectToAction("EditEpisode", new { id = EpisodeID });
        }
    }
}
