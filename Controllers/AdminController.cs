using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Podcast_MVC.Data;
using Podcast_MVC.Models.ViewModels;
using Podcast_MVC.Models;
using Podcast_MVC.Services;
using System.Security.Claims;

namespace Podcast_MVC.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly AwsService _awsService;
        public AdminController(
            ApplicationDbContext context,
            UserManager<User> userManager,
            RoleManager<IdentityRole> roleManager,
            AwsService awsService)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
            _awsService = awsService;
        }

        public IActionResult Dashboard()
        {
            var adminRole = _context.Roles.FirstOrDefault(r => r.Name == "Admin");
            var podcasterRole = _context.Roles.FirstOrDefault(r => r.Name == "Podcaster");
            var listenerRole = _context.Roles.FirstOrDefault(r => r.Name == "Listener");

            var vm = new AdminDashboardViewModel
            {
                // Summary counts
                PodcasterCount = podcasterRole == null ? 0 : _context.UserRoles.Count(r => r.RoleId == podcasterRole.Id),
                TotalPodcasts = _context.Podcasts.Count(),
                TotalEpisodes = _context.Episodes.Count(),

                // Episode status chart
                PendingEpisodes = _context.Episodes.Count(e => e.Status == EpisodeStatus.Pending),
                ApprovedEpisodes = _context.Episodes.Count(e => e.Status == EpisodeStatus.Approved),
                RejectedEpisodes = _context.Episodes.Count(e => e.Status == EpisodeStatus.Rejected),

                // Users by role chart
                AdminCount = adminRole == null ? 0 : _context.UserRoles.Count(r => r.RoleId == adminRole.Id),
                ListenerCount = listenerRole == null ? 0 : _context.UserRoles.Count(r => r.RoleId == listenerRole.Id)
            };

            return View(vm);
        }

        // GET: /Admin/Users
        public IActionResult Users()
        {
            // Get admin role
            var adminRole = _context.Roles.FirstOrDefault(r => r.Name == "Admin");

            // Get non-admin users
            var nonAdminUserIds = _context.UserRoles
                .Where(ur => ur.RoleId != adminRole.Id)
                .Select(ur => ur.UserId)
                .Distinct()
                .ToList();

            var users = _context.Users
                .Where(u => nonAdminUserIds.Contains(u.Id))
                .ToList();

            return View(users);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUser(string id)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id);
            if (user == null)
                return NotFound();

            var podcasts = await _context.Podcasts
                .Include(p => p.Episodes)
                .Where(p => p.CreatorID == id)
                .ToListAsync();

            var s3 = _awsService.S3Client;
            var bucket = _awsService.BucketName;

            // Collect all S3 keys first
            var keysToDelete = podcasts
                .SelectMany(p => p.Episodes)
                .Where(e => !string.IsNullOrEmpty(e.AudioFileURL))
                .Select(e => new Amazon.S3.Model.KeyVersion { Key = e.AudioFileURL })
                .ToList();

            if (keysToDelete.Any())
            {
                // S3 allows bulk delete up to 1000 objects at a time
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

            // Remove episodes and podcasts from DB
            foreach (var podcast in podcasts)
            {
                _context.Episodes.RemoveRange(podcast.Episodes);
            }
            _context.Podcasts.RemoveRange(podcasts);

            // Remove user
            _context.Users.Remove(user);

            await _context.SaveChangesAsync();

            return RedirectToAction("Users");
        }

        // GET: /Admin/ApproveEpisodes
        public IActionResult ApproveEpisodes()
        {
            var pendingEpisodes = _context.Episodes
                .Include(e => e.Podcast)
                .ThenInclude(p => p.Creator)
                .Where(e => e.Status == EpisodeStatus.Pending)
                .OrderByDescending(e => e.ReleaseDate)
                .Select(e => new EpisodeApprovalViewModel
                {
                    EpisodeID = e.EpisodeID,
                    Title = e.Title,
                    PodcastTitle = e.Podcast.Title,
                    CreatorName = e.Podcast.Creator.FullName,
                    CreatorEmail = e.Podcast.Creator.Email,
                    ReleaseDate = e.ReleaseDate,
                })
                .ToList();

            return View(pendingEpisodes);
        }


        // POST: /Admin/ApproveEpisode/{id}
        [HttpPost]
        public async Task<IActionResult> ApproveEpisode(int id)
        {
            var episode = await _context.Episodes.FindAsync(id);
            if (episode == null) return NotFound();

            episode.Status = EpisodeStatus.Approved;
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(ApproveEpisodes));
        }

        // POST: /Admin/RejectEpisode/{id}
        [HttpPost]
        public async Task<IActionResult> RejectEpisode(int id)
        {

            var episode = await _context.Episodes.FindAsync(id);
            if (episode == null) return NotFound();

            episode.Status = EpisodeStatus.Rejected;
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(ApproveEpisodes));
        }
    }
}
