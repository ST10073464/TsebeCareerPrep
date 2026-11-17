
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

        // ======================================================
        // GET: /Account/Register
        // PUBLIC CANDIDATE REGISTRATION
        // ======================================================

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Register(string? returnUrl = null)
        {
            var safeReturnUrl =
                !string.IsNullOrWhiteSpace(returnUrl) &&
                Url.IsLocalUrl(returnUrl)
                    ? returnUrl
                    : null;

            if (User.Identity?.IsAuthenticated == true)
            {
                if (safeReturnUrl != null)
                {
                    return LocalRedirect(safeReturnUrl);
                }

                return RedirectToAction("Index", "Home");
            }

            ViewData["ReturnUrl"] = safeReturnUrl;

            return View(new RegisterViewModel());
        }

        // ======================================================
        // POST: /Account/Register
        // CREATE A NEW CANDIDATE ACCOUNT
        // ======================================================

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(
            RegisterViewModel model,
            string? returnUrl = null)
        {
            // Only accept local return URLs.
            var safeReturnUrl =
                !string.IsNullOrWhiteSpace(returnUrl) &&
                Url.IsLocalUrl(returnUrl)
                    ? returnUrl
                    : null;

            ViewData["ReturnUrl"] = safeReturnUrl;

            // Prevent signed-in users from registering again.
            if (User.Identity?.IsAuthenticated == true)
            {
                if (safeReturnUrl != null)
                {
                    return LocalRedirect(safeReturnUrl);
                }

                return RedirectToAction("Index", "Home");
            }

            // Validate registration details.
            if (!ModelState.IsValid)
            {
                model.Password = string.Empty;
                model.ConfirmPassword = string.Empty;

                return View(model);
            }

            try
            {
                // Register the Candidate through the API.
                var result =
                    await _authenticationService.RegisterAsync(model);

                if (!result.Success)
                {
                    ModelState.AddModelError(
                        string.Empty,
                        result.Message);

                    model.Password = string.Empty;
                    model.ConfirmPassword = string.Empty;

                    return View(model);
                }

                // Registration succeeded.
                // Candidate must sign in before starting.
                TempData["RegistrationSuccess"] = result.Message;

                // Preserve the selected interview category.
                return RedirectToAction(
                    "Login",
                    "Account",
                    new { returnUrl = safeReturnUrl });
            }
            catch (HttpRequestException)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "The registration service is unavailable. Please try again.");
            }
            catch (TaskCanceledException)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "The registration request timed out. Please try again.");
            }
            catch (System.Text.Json.JsonException)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "The registration service returned an unexpected response. Please try again.");
            }

            // Clear passwords before returning the form.
            model.Password = string.Empty;
            model.ConfirmPassword = string.Empty;

            return View(model);
        }

        // ======================================================
        // GET: /Account/Login
        // EXISTING LOGIN FUNCTIONALITY
        // ======================================================

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] =
                !string.IsNullOrWhiteSpace(returnUrl) &&
                Url.IsLocalUrl(returnUrl)
                    ? returnUrl
                    : null;

            return View(new LoginViewModel());
        }

        // ======================================================
        // POST: /Account/Login
        // ======================================================

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

            // Read the API authentication cookie.
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

            // Create MVC authentication claims.
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

            var identity = new ClaimsIdentity(
                claims,
                IdentityConstants.ApplicationScheme);

            var principal = new ClaimsPrincipal(identity);

            // Issue the MVC authentication cookie.
            await HttpContext.SignInAsync(
                IdentityConstants.ApplicationScheme,
                principal,
                new AuthenticationProperties
                {
                    IsPersistent = false,
                    AllowRefresh = true
                });

            // Return to the selected interview category
            // when a valid local return URL was supplied.
            if (!string.IsNullOrWhiteSpace(returnUrl) &&
                Url.IsLocalUrl(returnUrl))
            {
                return LocalRedirect(returnUrl);
            }

            // Preserve the existing Administrator redirect.

            if (string.Equals(
                    loginResult.User.Role,
                    "SuperAdministrator",
                    StringComparison.OrdinalIgnoreCase) ||
                string.Equals(
                    loginResult.User.Role,
                    "Administrator",
                    StringComparison.OrdinalIgnoreCase))
            {
                return RedirectToAction("Index", "Admin");
            }



            // Candidates go to the homepage by default.
            return RedirectToAction("Index", "Home");
        }

        // ======================================================
        // GET: /Account/AccessDenied
        // ======================================================

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }

        // ======================================================
        // POST: /Account/Logout
        // ======================================================

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(
                IdentityConstants.ApplicationScheme);

            Response.Cookies.Delete(
                "TsebeCareerPrep.ApiAuth");

            return RedirectToAction(
                "Login",
                "Account");
        }
    }
}
