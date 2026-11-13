
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Net.Http.Json;
using TsebeCareerPrep.Mvc.Models;

namespace TsebeCareerPrep.Mvc.Controllers
{
    [Authorize(Roles = "Administrator")]
    public class AdminUsersController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public AdminUsersController(
            IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        // GET: /AdminUsers
        public async Task<IActionResult> Index()
        {
            var client = _httpClientFactory.CreateClient(
                "AuthenticatedInterviewApi");

            try
            {
                var response = await client.GetAsync(
                    "api/AdminUsers");

                if (response.StatusCode ==
                    HttpStatusCode.Unauthorized)
                {
                    return Challenge();
                }

                if (response.StatusCode ==
                    HttpStatusCode.Forbidden)
                {
                    return Forbid();
                }

                if (!response.IsSuccessStatusCode)
                {
                    TempData["ErrorMessage"] =
                        "Unable to retrieve registered users.";

                    return View(
                        new List<AdminUserViewModel>());
                }

                var users = await response.Content
                    .ReadFromJsonAsync<List<AdminUserViewModel>>();

                return View(
                    users ?? new List<AdminUserViewModel>());
            }
            catch (HttpRequestException)
            {
                TempData["ErrorMessage"] =
                    "The user management service is unavailable.";

                return View(
                    new List<AdminUserViewModel>());
            }
        }

        // POST: /AdminUsers/Unlock/{id}
        // Administrator: Unlock a temporarily locked account.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Unlock(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                TempData["ErrorMessage"] =
                    "A valid user account is required.";

                return RedirectToAction(nameof(Index));
            }

            var client = _httpClientFactory.CreateClient(
                "AuthenticatedInterviewApi");

            try
            {
                var response = await client.PostAsync(
                    $"api/AdminUsers/{Uri.EscapeDataString(id)}/unlock",
                    null);

                if (response.StatusCode ==
                    HttpStatusCode.Unauthorized)
                {
                    return Challenge();
                }

                if (response.StatusCode ==
                    HttpStatusCode.Forbidden)
                {
                    return Forbid();
                }

                if (response.IsSuccessStatusCode)
                {
                    TempData["SuccessMessage"] =
                        "User account unlocked successfully.";
                }
                else if (response.StatusCode ==
                         HttpStatusCode.NotFound)
                {
                    TempData["ErrorMessage"] =
                        "The selected user account was not found.";
                }
                else if (response.StatusCode ==
                         HttpStatusCode.BadRequest)
                {
                    TempData["ErrorMessage"] =
                        "The account is not locked or could not be unlocked.";
                }
                else
                {
                    TempData["ErrorMessage"] =
                        "Unable to unlock the user account.";
                }
            }
            catch (HttpRequestException)
            {
                TempData["ErrorMessage"] =
                    "The user management service is unavailable.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
