
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TsebeCareerPrep.Mvc.Models;

using AuthenticationService =
    TsebeCareerPrep.Mvc.Services.AuthenticationService;

using ApiLoginResult =
    TsebeCareerPrep.Mvc.Services.ApiLoginResult;

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
            catch (InvalidOperationException)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Authentication succeeded, but the server could not complete the sign-in. Please try again.");

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

            // Read the authentication cookie issued by the API.
            var cookieParts =
                loginResult.AuthenticationCookie.Split('=', 2);

            if (cookieParts.Length != 2 ||
                !string.Equals(
                    cookieParts[0],
                    "TsebeCareerPrep.Auth",
                    StringComparison.Ordinal) ||
                string.IsNullOrWhiteSpace(cookieParts[1]))
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Authentication could not be completed.");

                model.Password = string.Empty;
                return View(model);
            }

            // Store the API authentication ticket separately.
            Response.Cookies.Append(
                "TsebeCareerPrep.ApiAuth",
                cookieParts[1],
                new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Lax,
                    Path = "/",
                    IsEssential = true
                });

            // Create the MVC authentication claims.
            var claims = new List<Claim>
            {
                new Claim(
                    ClaimTypes.NameIdentifier,
                    loginResult.User.UserId),

                new Claim(
                    ClaimTypes.Name,
                    loginResult.User.Email),

                new Claim(
                    ClaimTypes.Email,
                    loginResult.User.Email),

                new Claim(
                    ClaimTypes.Role,
                    loginResult.User.Role)
            };

            // Create the MVC authentication identity.
            var identity = new ClaimsIdentity(
                claims,
                IdentityConstants.ApplicationScheme);

            var principal = new ClaimsPrincipal(identity);

            // Issue a separate MVC authentication cookie.
            await HttpContext.SignInAsync(
                IdentityConstants.ApplicationScheme,
                principal,
                new AuthenticationProperties
                {
                    IsPersistent = false,
                    AllowRefresh = true
                });

            // Respect valid local return URLs.
            if (!string.IsNullOrWhiteSpace(returnUrl) &&
                Url.IsLocalUrl(returnUrl))
            {
                return LocalRedirect(returnUrl);
            }

            // Redirect administrators to their dashboard.
            if (string.Equals(
                loginResult.User.Role,
                "Administrator",
                StringComparison.OrdinalIgnoreCase))
            {
                return RedirectToAction("Index", "Admin");
            }

            // Candidates continue to the normal homepage.
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
            // Sign out of MVC.
            await HttpContext.SignOutAsync(
                IdentityConstants.ApplicationScheme);

            // Remove the separate API authentication ticket.
            Response.Cookies.Delete(
                "TsebeCareerPrep.ApiAuth");

            return RedirectToAction(
                "Login",
                "Account");
        }
    }
}
