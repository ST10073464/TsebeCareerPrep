
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

        // ======================================================
        // GET: /
        // PUBLIC LANDING PAGE AND AUTHENTICATED REDIRECTION
        // ======================================================

        public IActionResult Index()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                // SuperAdministrators and Administrators
                // go directly to the administration dashboard.
                if (User.IsInRole("SuperAdministrator") ||
                    User.IsInRole("Administrator"))
                {
                    return RedirectToAction(
                        "Index",
                        "Admin");
                }

                // Candidates go directly to their
                // interview categories.
                if (User.IsInRole("Candidate"))
                {
                    return RedirectToAction(
                        "Index",
                        "InterviewCategories");
                }
            }

            // Only unauthenticated visitors see
            // the public landing page.
            return View();
        }

        // GET: /Home/Candidate
        // Allows administrators to enter the Candidate area
        // without changing their account role.
        [Microsoft.AspNetCore.Authorization.Authorize(
            Roles = "SuperAdministrator")]
        [HttpGet]
        public IActionResult Candidate()
        {
            return RedirectToAction(
                "Index",
                "InterviewCategories");
        }

        // ======================================================
        // GET: /Home/Privacy
        // ======================================================

        public IActionResult Privacy()
        {
            return View();
        }

        // ======================================================
        // APPLICATION ERROR PAGE
        // ======================================================

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
