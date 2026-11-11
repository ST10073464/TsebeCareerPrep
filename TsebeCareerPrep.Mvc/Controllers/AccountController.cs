
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using TsebeCareerPrep.Mvc.Models;

using AuthenticationService = TsebeCareerPrep.Mvc.Services.AuthenticationService;
using ApiLoginResult = TsebeCareerPrep.Mvc.Services.ApiLoginResult;


namespace TsebeCareerPrep.Mvc.Controllers
{
    public class AccountController : Controller
    {
        private readonly AuthenticationService _authenticationService;

        public AccountController(
            AuthenticationService authenticationService)
        {
            _authenticationService = authenticationService;
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            return View(new LoginViewModel());
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(
            LoginViewModel model,
            string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (!ModelState.IsValid)
            {
                model.Password = string.Empty;
                return View(model);
            }

            ApiLoginResult? loginResult;

            try
            {
                loginResult =
                    await _authenticationService.LoginAsync(model);
            }
            catch (HttpRequestException)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "The authentication service is unavailable. Please try again.");

                model.Password = string.Empty;
                return View(model);
            }
            catch (TaskCanceledException)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "The login request timed out. Please try again.");

                model.Password = string.Empty;
                return View(model);
            }

            if (loginResult == null)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Invalid email or password.");

                model.Password = string.Empty;
                return View(model);
            }

            // Use the Identity authentication cookie
            // issued by the API.
            var cookieParts =
                loginResult.AuthenticationCookie.Split('=', 2);

            if (cookieParts.Length != 2 ||
                string.IsNullOrWhiteSpace(cookieParts[1]))
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Authentication could not be completed.");

                model.Password = string.Empty;
                return View(model);
            }

            Response.Cookies.Append(
                cookieParts[0],
                cookieParts[1],
                new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Lax,
                    Path = "/",
                    IsEssential = true
                });

            if (!string.IsNullOrWhiteSpace(returnUrl) &&
                Url.IsLocalUrl(returnUrl))
            {
                return LocalRedirect(returnUrl);
            }

            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(
                IdentityConstants.ApplicationScheme);

            return RedirectToAction("Login", "Account");
        }
    }
}
