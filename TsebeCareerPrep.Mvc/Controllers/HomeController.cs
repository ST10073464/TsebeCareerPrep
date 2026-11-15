
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using TsebeCareerPrep.Mvc.Models;

namespace TsebeCareerPrep.Mvc.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(
            ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        // GET: /
        public IActionResult Index()
        {
            // Administrators should always land on
            // their administration dashboard.
            if (User.Identity?.IsAuthenticated == true &&
                User.IsInRole("Administrator"))
            {
                return RedirectToAction(
                    "Index",
                    "Admin");
            }

            // Candidates and unauthenticated visitors
            // continue to the normal homepage.
            return View();
        }

        // GET: /Home/Privacy
        public IActionResult Privacy()
        {
            return View();
        }

        // Application error page.
        [ResponseCache(
            Duration = 0,
            Location = ResponseCacheLocation.None,
            NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel
            {
                RequestId =
                    Activity.Current?.Id ??
                    HttpContext.TraceIdentifier
            });
        }
    }
}
