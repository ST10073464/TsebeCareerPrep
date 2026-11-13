
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using TsebeCareerPrep.Mvc.Models;

namespace TsebeCareerPrep.Mvc.Controllers
{
    [Authorize(Roles = "Administrator")]
    public class AdminCategoriesController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public AdminCategoriesController(
            IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        // =====================================================
        // GET: /AdminCategories
        // Display active and inactive interview categories.
        // =====================================================
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var client = _httpClientFactory.CreateClient(
                "AuthenticatedInterviewApi");

            HttpResponseMessage response;

            try
            {
                response = await client.GetAsync(
                    "api/InterviewCategories/admin/all");
            }
            catch (HttpRequestException)
            {
                return StatusCode(
                    503,
                    "The interview API is currently unavailable.");
            }
            catch (TaskCanceledException)
            {
                return StatusCode(
                    504,
                    "The interview API request timed out.");
            }

            using (response)
            {
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
                    return StatusCode(
                        (int)response.StatusCode,
                        "Unable to retrieve interview categories.");
                }

                try
                {
                    var categories = await response.Content
                        .ReadFromJsonAsync<
                            List<InterviewCategoryAdminViewModel>>();

                    return View(categories ??
                        new List<InterviewCategoryAdminViewModel>());
                }
                catch (JsonException)
                {
                    return StatusCode(
                        502,
                        "The interview API returned an invalid response.");
                }
            }
        }


        // =====================================================
        // GET: /AdminCategories/Create
        // Display the Create Category form.
        // =====================================================
        [HttpGet]
        public IActionResult Create()
        {
            return View(new InterviewCategoryAdminViewModel());
        }

        // =====================================================
        // POST: /AdminCategories/Create
        // Create a new interview category.
        // =====================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            InterviewCategoryAdminViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            model.Name = model.Name.Trim();
            model.Description = model.Description.Trim();

            var client = _httpClientFactory.CreateClient(
                "AuthenticatedInterviewApi");

            HttpResponseMessage response;

            try
            {
                response = await client.PostAsJsonAsync(
                    "api/InterviewCategories/admin",
                    new
                    {
                        model.Name,
                        model.Description,
                        model.IsActive
                    });
            }
            catch (HttpRequestException)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "The interview API is unavailable. Please try again.");

                return View(model);
            }
            catch (TaskCanceledException)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "The request timed out. Please try again.");

                return View(model);
            }

            using (response)
            {
                if (response.StatusCode == HttpStatusCode.Unauthorized)
                    return Challenge();

                if (response.StatusCode == HttpStatusCode.Forbidden)
                    return Forbid();

                if (response.IsSuccessStatusCode)
                {
                    TempData["SuccessMessage"] =
                        "Interview category created successfully.";

                    return RedirectToAction(nameof(Index));
                }

                if (response.StatusCode == HttpStatusCode.Conflict)
                {
                    ModelState.AddModelError(
                        nameof(model.Name),
                        "A category with this name already exists.");

                    return View(model);
                }

                if (response.StatusCode == HttpStatusCode.BadRequest)
                {
                    ModelState.AddModelError(
                        string.Empty,
                        "Please check the category details and try again.");

                    return View(model);
                }

                ModelState.AddModelError(
                    string.Empty,
                    "Unable to create the interview category. Please try again.");

                return View(model);
            }
        }


        // =====================================================
        // GET: /AdminCategories/Edit/8
        // Retrieve a category for editing.
        // =====================================================
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            if (id <= 0)
                return BadRequest("Invalid category ID.");

            var client = _httpClientFactory.CreateClient(
                "AuthenticatedInterviewApi");

            HttpResponseMessage response;

            try
            {
                response = await client.GetAsync(
                    "api/InterviewCategories/admin/all");
            }
            catch (HttpRequestException)
            {
                return StatusCode(503,
                    "The interview API is currently unavailable.");
            }
            catch (TaskCanceledException)
            {
                return StatusCode(504,
                    "The interview API request timed out.");
            }

            using (response)
            {
                if (response.StatusCode == HttpStatusCode.Unauthorized)
                    return Challenge();

                if (response.StatusCode == HttpStatusCode.Forbidden)
                    return Forbid();

                if (!response.IsSuccessStatusCode)
                {
                    return StatusCode(
                        (int)response.StatusCode,
                        "Unable to retrieve interview categories.");
                }

                List<InterviewCategoryAdminViewModel>? categories;

                try
                {
                    categories = await response.Content
                        .ReadFromJsonAsync<
                            List<InterviewCategoryAdminViewModel>>();
                }
                catch (JsonException)
                {
                    return StatusCode(502,
                        "The interview API returned an invalid response.");
                }

                var category = categories?
                    .FirstOrDefault(c => c.Id == id);

                if (category == null)
                    return NotFound("Interview category not found.");

                return View(category);
            }
        }

        // =====================================================
        // POST: /AdminCategories/Edit/8
        // Update category details and active status.
        // =====================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            InterviewCategoryAdminViewModel model)
        {
            if (id <= 0 || id != model.Id)
                return BadRequest("Invalid category ID.");

            if (!ModelState.IsValid)
                return View(model);

            model.Name = model.Name.Trim();
            model.Description = model.Description.Trim();

            var client = _httpClientFactory.CreateClient(
                "AuthenticatedInterviewApi");

            HttpResponseMessage response;

            try
            {
                response = await client.PutAsJsonAsync(
                    $"api/InterviewCategories/admin/{id}",
                    new
                    {
                        model.Name,
                        model.Description,
                        model.IsActive
                    });
            }
            catch (HttpRequestException)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "The interview API is unavailable. Please try again.");

                return View(model);
            }
            catch (TaskCanceledException)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "The request timed out. Please try again.");

                return View(model);
            }

            using (response)
            {
                if (response.StatusCode == HttpStatusCode.Unauthorized)
                    return Challenge();

                if (response.StatusCode == HttpStatusCode.Forbidden)
                    return Forbid();

                if (response.StatusCode == HttpStatusCode.NotFound)
                    return NotFound("Interview category not found.");

                if (response.IsSuccessStatusCode)
                {
                    TempData["SuccessMessage"] =
                        "Interview category updated successfully.";

                    return RedirectToAction(nameof(Index));
                }

                if (response.StatusCode == HttpStatusCode.Conflict)
                {
                    ModelState.AddModelError(
                        nameof(model.Name),
                        "Another category already uses this name.");

                    return View(model);
                }

                if (response.StatusCode == HttpStatusCode.BadRequest)
                {
                    ModelState.AddModelError(
                        string.Empty,
                        "Please check the category details and try again.");

                    return View(model);
                }

                ModelState.AddModelError(
                    string.Empty,
                    "Unable to update the interview category. Please try again.");

                return View(model);
            }
        }


    }
}
