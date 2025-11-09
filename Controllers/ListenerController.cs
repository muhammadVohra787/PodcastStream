using Amazon.S3.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Podcast_MVC.Data;
using Podcast_MVC.Models.ViewModels;
using Podcast_MVC.Models;
using Podcast_MVC.Services;
using System.Security.Claims;
using Amazon.S3;
using NAudio.Wave;
using static Podcast_MVC.Models.ViewModels.EpisodeCardViewModel;


namespace Podcast_MVC.Controllers
{
    [Authorize(Roles = "Listener")]
    public class ListenerController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly AwsService _awsService;
        public ListenerController(ApplicationDbContext context, AwsService awsService)
        {
            _context = context;
            _awsService = awsService;
        }

        public IActionResult Dashboard(string searchQuery = "", string searchType = "episode")
        {
            // Featured episodes
            var topEpisodes = _context.Episodes
                .Include(e => e.Podcast)
                .Where(e => e.Status == EpisodeStatus.Approved)
                .OrderByDescending(e => e.PlayCount)
                .Take(3)
                .Select(e => new EpisodeCardViewModel
                {
                    EpisodeID = e.EpisodeID,
                    Title = e.Title,
                    PodcastTitle = e.Podcast.Title,
                    PodcastId = e.PodcastID,
                    PlayCount = e.PlayCount,
                    ReleaseDate = e.ReleaseDate
                })
                .ToList();

            var recentEpisodes = _context.Episodes
                .Include(e => e.Podcast)
                .Where(e => e.Status == EpisodeStatus.Approved)
                .OrderByDescending(e => e.ReleaseDate)
                .Take(3)
                .Select(e => new EpisodeCardViewModel
                {
                    EpisodeID = e.EpisodeID,
                    Title = e.Title,
                    PodcastTitle = e.Podcast.Title,
                    PodcastId = e.PodcastID,
                    PlayCount = e.PlayCount,
                    ReleaseDate = e.ReleaseDate
                })
                .ToList();

            // Base query for podcasts
            var podcastQuery = _context.Podcasts
                .Include(p => p.Episodes.Where(e => e.Status == EpisodeStatus.Approved))
                .Where(p => p.Episodes.Any(e => e.Status == EpisodeStatus.Approved));

            // Apply search if any
            if (!string.IsNullOrEmpty(searchQuery))
            {
                var searchTerm = $"%{searchQuery}%";

                if (searchType == "author")
                {
                    // Search by podcaster's name
                    podcastQuery = podcastQuery
                        .Include(p => p.Creator)
                        .Where(p => p.Creator != null && EF.Functions.Like(p.Creator.FullName, searchTerm));
                }
                else // episode search
                {
                    // Only match episode titles
                    podcastQuery = podcastQuery
                        .Where(p => p.Episodes.Any(e => EF.Functions.Like(e.Title, searchTerm)));
                }

                // Hide featured lists during search
                topEpisodes.Clear();
                recentEpisodes.Clear();
            }

            // Build results
            var podcasts = podcastQuery
                .OrderBy(p => p.Title)
                .Select(p => new PodcastWithEpisodesViewModel
                {
                    PodcastID = p.PodcastID,
                    Title = p.Title,
                    Episodes = (string.IsNullOrEmpty(searchQuery) || searchType == "author")
                        ? p.Episodes
                            .OrderByDescending(e => e.ReleaseDate)
                            .Select(e => new EpisodeCardViewModel
                            {
                                EpisodeID = e.EpisodeID,
                                Title = e.Title,
                                PodcastTitle = p.Title,
                                PlayCount = e.PlayCount,
                                ReleaseDate = e.ReleaseDate,
                                PodcastId = e.PodcastID
                            })
                            .ToList()
                        : p.Episodes
                            .Where(e => EF.Functions.Like(e.Title, $"%{searchQuery}%"))
                            .OrderByDescending(e => e.ReleaseDate)
                            .Select(e => new EpisodeCardViewModel
                            {
                                EpisodeID = e.EpisodeID,
                                Title = e.Title,
                                PodcastTitle = p.Title,
                                PlayCount = e.PlayCount,
                                ReleaseDate = e.ReleaseDate,
                                PodcastId = e.PodcastID,
                            })
                            .ToList()
                })
                .ToList();

            var vm = new ListenerDashboardViewModel
            {
                SearchQuery = searchQuery,
                SearchType = searchType,
                TopEpisodes = topEpisodes,
                RecentEpisodes = recentEpisodes,
                Podcasts = podcasts
            };

            return View(vm);
        }

        public async Task<IActionResult> ViewEpisode(int episodeId, int podcastId)
        {
            Console.WriteLine($"Viewing EpisodeID: {episodeId} from PodcastID: {podcastId}");
            var episode = _context.Episodes
                .Include(e => e.Podcast)
                    .ThenInclude(p => p.Creator) 
                .Include(e => e.Podcast)
                    .ThenInclude(p => p.Episodes)
                .FirstOrDefault(e => e.EpisodeID == episodeId && e.PodcastID == podcastId);

            if (episode == null || episode.Podcast == null)
                return NotFound();

            // Generate a signed URL for the audio
            string audioUrl = null;
            if (!string.IsNullOrEmpty(episode.AudioFileURL))
            {
                var request = new GetPreSignedUrlRequest
                {
                    BucketName = _awsService.BucketName,
                    Key = episode.AudioFileURL,
                    Expires = DateTime.UtcNow.AddMinutes(3)
                };
                audioUrl = _awsService.S3Client.GetPreSignedURL(request);
                Console.WriteLine($"Audio Key. {episode.AudioFileURL}");
                Console.WriteLine($"Generated signed URL for EpisodeID {audioUrl}");
            }
            var comments = await _awsService.DbContext
                .QueryAsync<Comment>(episode.EpisodeID)
                .GetRemainingAsync();

            var vm = new ViewEpisodeViewModel
            {
                EpisodeID = episode.EpisodeID,
                Title = episode.Title,
                PodcastTitle = episode.Podcast.Title,
                PodcastId = episode.PodcastID,
                PodcastDescription= episode.Podcast.Description,
                PlayCount = episode.PlayCount,
                Duration = episode.Duration,
                AudioFileURL = audioUrl,
                ReleaseDate = episode.ReleaseDate,
                CreatorName = episode.Podcast.Creator.FullName,
                CreatorEpisodeCount = episode.Podcast.Episodes.Count(e => e.Status == EpisodeStatus.Approved),
                IsSubcribed = _context.Subscriptions.Any(s => s.PodcastID == podcastId && s.UserID == User.FindFirstValue(ClaimTypes.NameIdentifier)),
                Comments = comments.Count > 0 ? comments.ToList() : new List<Comment>()
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddComment(int EpisodeID, string Content)
        {
            if (string.IsNullOrWhiteSpace(Content))
                return RedirectToAction("ViewEpisode", new { episodeId = EpisodeID });

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var episode = await _context.Episodes
                .Include(e => e.Podcast)
                .FirstOrDefaultAsync(e => e.EpisodeID == EpisodeID);

            if (episode == null)
                return NotFound();

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
                await _awsService.DbContext.SaveAsync(comment);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to add comment: {ex.Message}");
                TempData["Error"] = "Failed to add comment.";
            }

            // Redirect back to the same page with updated comments
            return RedirectToAction("ViewEpisode", new { episodeId = EpisodeID, podcastId = episode.PodcastID });
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditComment(string commentId, string newContent, int episodeId)
        {
            if (string.IsNullOrWhiteSpace(newContent))
                return RedirectToAction("ViewEpisode", new { episodeId });

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Load the comment from DynamoDB
            var db = _awsService.DbContext;
            var comment = await db.LoadAsync<Comment>(episodeId, commentId);

            if (comment == null)
                return NotFound();

            // Only allow the comment owner to edit
            if (comment.UserID != userId)
                return Forbid();

            // Update the text and timestamp
            comment.Text = newContent;
            comment.Timestamp = DateTime.UtcNow;

            try
            {
                await db.SaveAsync(comment);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to edit comment: {ex.Message}");
                TempData["Error"] = "Failed to edit comment.";
            }

            // Redirect back to the episode view with updated comments
            return RedirectToAction("ViewEpisode", new { episodeId, podcastId = comment.PodcastID });
        }

        [HttpPost]
        public IActionResult RecordView(int episodeId, int podcastId)
        {
            Console.WriteLine($"Recording view for EpisodeID: {episodeId} from PodcastID: {podcastId}");
            _context.Episodes
                .Where(e => e.EpisodeID == episodeId && e.PodcastID == podcastId)
                .ExecuteUpdate(e => e.SetProperty(ep => ep.PlayCount, ep => ep.PlayCount + 1));

            return Ok();
        }
        [HttpPost]
        public IActionResult Subscribe(int podcastId)
        {
            // Get the logged-in user ID
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            // Check if the subscription already exists
            var exists = _context.Subscriptions
                .Any(s => s.PodcastID == podcastId && s.UserID == userId);

            if (exists)
                return BadRequest("Already subscribed.");

            // Create new subscription
            var subscription = new Subscription
            {
                PodcastID = podcastId,
                UserID = userId,
                SubscribedDate = DateTime.UtcNow
            };

            _context.Subscriptions.Add(subscription);
            _context.SaveChanges();

            return Ok(new { message = "Subscribed successfully!" });
        }

        [HttpPost]
        public IActionResult Unsubscribe(int podcastId)
        {
            // Get the logged-in user ID
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            // Find the subscription
            var subscription = _context.Subscriptions
                .FirstOrDefault(s => s.PodcastID == podcastId && s.UserID == userId);
            if (subscription == null)
                return NotFound("Subscription not found.");
            _context.Subscriptions.Remove(subscription);
            _context.SaveChanges();

            return Ok(new { message = "Subscribed successfully!" });
        }
    }
}
