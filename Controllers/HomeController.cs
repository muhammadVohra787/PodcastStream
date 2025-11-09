using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Podcast_MVC.Models.ViewModels;
using Podcast_MVC.Models;
using System.Diagnostics;

namespace Podcast_MVC.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly SignInManager<User> _signInManager;
        private readonly UserManager<User> _userManager;

        public HomeController(
            ILogger<HomeController> logger,
            SignInManager<User> signInManager,
            UserManager<User> userManager)
        {
            _logger = logger;
            _signInManager = signInManager;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            // If user is not logged in, show normal home page
            if (!User.Identity.IsAuthenticated)
                return View();

            // Otherwise, get current user
            var user = await _userManager.GetUserAsync(User);

            // Redirect based on role
            if (await _userManager.IsInRoleAsync(user, "Podcaster"))
            {
                return RedirectToAction("Dashboard", "Podcaster");
            }
            else if (await _userManager.IsInRoleAsync(user, "Admin"))
            {
                return RedirectToAction("Dashboard", "Admin");
            }
            else
            {
                return RedirectToAction("Dashboard", "Listener");
            }
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
