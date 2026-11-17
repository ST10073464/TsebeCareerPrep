
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Net.Http.Json;
using TsebeCareerPrep.Mvc.Models;

namespace TsebeCareerPrep.Mvc.Controllers
{

    [Authorize(Roles = "Administrator,SuperAdministrator")]

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

                    TempData["HighlightUserId"] = id;
                    TempData["HighlightType"] = "status";
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

        // POST: /AdminUsers/Promote/{id}
        [HttpPost]
        [Authorize(Roles = "SuperAdministrator")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Promote(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                TempData["ErrorMessage"] = "A valid user account is required.";
                return RedirectToAction(nameof(Index));
            }

            var client = _httpClientFactory.CreateClient("AuthenticatedInterviewApi");

            try
            {
                var response = await client.PostAsync(
                    $"api/AdminUsers/{Uri.EscapeDataString(id)}/promote", null);

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                    return Challenge();
                if (response.StatusCode == HttpStatusCode.Forbidden)
                    return Forbid();

                if (response.IsSuccessStatusCode)
                {
                    TempData["SuccessMessage"] = "Administrator access granted successfully.";
                    TempData["HighlightUserId"] = id;
                    TempData["HighlightType"] = "status";
                }
                else if (response.StatusCode == HttpStatusCode.NotFound)
                    TempData["ErrorMessage"] = "The selected user account was not found.";
                else if (response.StatusCode == HttpStatusCode.Conflict)
                    TempData["ErrorMessage"] = "This user already has an administrative role.";
                else if (response.StatusCode == HttpStatusCode.BadRequest)
                    TempData["ErrorMessage"] = "Only candidate accounts can be promoted.";
                else
                    TempData["ErrorMessage"] = "Unable to grant administrator access.";
            }
            catch (HttpRequestException)
            {
                TempData["ErrorMessage"] = "The user management service is unavailable.";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [Authorize(Roles = "SuperAdministrator")]
        [ValidateAntiForgeryToken]
        public Task<IActionResult> Demote(string id) =>
            ApplyAdminChange(id, "demote", "Administrator access removed.");

        [HttpPost]
        [Authorize(Roles = "SuperAdministrator")]
        [ValidateAntiForgeryToken]
        public Task<IActionResult> Deactivate(string id) =>
            ApplyAdminChange(id, "deactivate", "Administrator account deleted. The account ID and history are preserved.");

        private async Task<IActionResult> ApplyAdminChange(string id, string operation, string successMessage)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                TempData["ErrorMessage"] = "A valid user account is required.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var client = _httpClientFactory.CreateClient("AuthenticatedInterviewApi");
                var response = await client.PostAsync(
                    $"api/AdminUsers/{Uri.EscapeDataString(id)}/{operation}", null);

                if (response.StatusCode == HttpStatusCode.Unauthorized) return Challenge();
                if (response.StatusCode == HttpStatusCode.Forbidden) return Forbid();
                if (response.IsSuccessStatusCode)
                {
                    TempData["SuccessMessage"] = successMessage;
                    TempData["HighlightUserId"] = id;
                    TempData["HighlightType"] = "status";
                }
                else if (response.StatusCode == HttpStatusCode.NotFound)
                    TempData["ErrorMessage"] = "The selected user account was not found.";
                else if (response.StatusCode == HttpStatusCode.Conflict)
                    TempData["ErrorMessage"] = "This account is not eligible for that action.";
                else if (response.StatusCode == HttpStatusCode.BadRequest)
                    TempData["ErrorMessage"] = "That account action could not be completed.";
                else
                    TempData["ErrorMessage"] = "Unable to update the administrator account.";
            }
            catch (HttpRequestException)
            {
                TempData["ErrorMessage"] = "The user management service is unavailable.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
