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

        public AdminCategoriesController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        // GET: /AdminCategories
        // Include inactive categories and questions in administrator statistics.
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var client = _httpClientFactory.CreateClient("AuthenticatedInterviewApi");

            try
            {
                using var categoriesResponse = await client.GetAsync(
                    "api/InterviewCategories/admin/all");

                if (categoriesResponse.StatusCode == HttpStatusCode.Unauthorized)
                    return Challenge();

                if (categoriesResponse.StatusCode == HttpStatusCode.Forbidden)
                    return Forbid();

                if (!categoriesResponse.IsSuccessStatusCode)
                    return StatusCode((int)categoriesResponse.StatusCode,
                        "Unable to retrieve interview categories.");

                var categories = await categoriesResponse.Content
                    .ReadFromJsonAsync<List<InterviewCategoryAdminViewModel>>()
                    ?? new List<InterviewCategoryAdminViewModel>();

                using var questionsResponse = await client.GetAsync(
                    "api/InterviewQuestions/admin/all");

                if (questionsResponse.StatusCode == HttpStatusCode.Unauthorized)
                    return Challenge();

                if (questionsResponse.StatusCode == HttpStatusCode.Forbidden)
                    return Forbid();

                if (!questionsResponse.IsSuccessStatusCode)
                    return StatusCode((int)questionsResponse.StatusCode,
                        "Unable to retrieve interview questions for category statistics.");

                var questions = await questionsResponse.Content
                    .ReadFromJsonAsync<List<InterviewQuestionViewModel>>()
                    ?? new List<InterviewQuestionViewModel>();

                var questionGroups = questions
                    .GroupBy(question => question.InterviewCategoryId)
                    .ToDictionary(group => group.Key, group => group.ToList());

                foreach (var category in categories)
                {
                    if (!questionGroups.TryGetValue(category.Id, out var categoryQuestions))
                        continue;

                    category.TotalQuestions = categoryQuestions.Count;
                    category.BeginnerQuestions = categoryQuestions.Count(q =>
                        string.Equals(q.DifficultyLevel?.Trim(), "Beginner",
                            StringComparison.OrdinalIgnoreCase));
                    category.IntermediateQuestions = categoryQuestions.Count(q =>
                        string.Equals(q.DifficultyLevel?.Trim(), "Intermediate",
                            StringComparison.OrdinalIgnoreCase));
                    category.AdvancedQuestions = categoryQuestions.Count(q =>
                        string.Equals(q.DifficultyLevel?.Trim(), "Advanced",
                            StringComparison.OrdinalIgnoreCase));
                }

                return View(categories);
            }
            catch (HttpRequestException)
            {
                return StatusCode(503, "The interview API is currently unavailable.");
            }
            catch (TaskCanceledException)
            {
                return StatusCode(504, "The interview API request timed out.");
            }
            catch (JsonException)
            {
                return StatusCode(502, "The interview API returned an invalid response.");
            }
        }

        // GET: /AdminCategories/Create
        [HttpGet]
        public IActionResult Create()
        {
            return View(new InterviewCategoryAdminViewModel());
        }

        // POST: /AdminCategories/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(InterviewCategoryAdminViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            model.Name = model.Name.Trim();
            model.Description = model.Description.Trim();

            var client = _httpClientFactory.CreateClient("AuthenticatedInterviewApi");
            HttpResponseMessage response;

            try
            {
                response = await client.PostAsJsonAsync(
                    "api/InterviewCategories/admin",
                    new { model.Name, model.Description, model.IsActive });
            }
            catch (HttpRequestException)
            {
                ModelState.AddModelError(string.Empty,
                    "The interview API is unavailable. Please try again.");
                return View(model);
            }
            catch (TaskCanceledException)
            {
                ModelState.AddModelError(string.Empty,
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
                    ModelState.AddModelError(nameof(model.Name),
                        "A category with this name already exists.");
                    return View(model);
                }

                if (response.StatusCode == HttpStatusCode.BadRequest)
                {
                    ModelState.AddModelError(string.Empty,
                        "Please check the category details and try again.");
                    return View(model);
                }

                ModelState.AddModelError(string.Empty,
                    "Unable to create the interview category. Please try again.");
                return View(model);
            }
        }

        // GET: /AdminCategories/Edit/8
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            if (id <= 0)
                return BadRequest("Invalid category ID.");

            var client = _httpClientFactory.CreateClient("AuthenticatedInterviewApi");
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
                    return StatusCode((int)response.StatusCode,
                        "Unable to retrieve interview categories.");

                List<InterviewCategoryAdminViewModel>? categories;
                try
                {
                    categories = await response.Content
                        .ReadFromJsonAsync<List<InterviewCategoryAdminViewModel>>();
                }
                catch (JsonException)
                {
                    return StatusCode(502,
                        "The interview API returned an invalid response.");
                }

                var category = categories?.FirstOrDefault(c => c.Id == id);
                if (category == null)
                    return NotFound("Interview category not found.");

                return View(category);
            }
        }

        // POST: /AdminCategories/Edit/8
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id, InterviewCategoryAdminViewModel model)
        {
            if (id <= 0 || id != model.Id)
                return BadRequest("Invalid category ID.");

            if (!ModelState.IsValid)
                return View(model);

            model.Name = model.Name.Trim();
            model.Description = model.Description.Trim();

            var client = _httpClientFactory.CreateClient("AuthenticatedInterviewApi");
            HttpResponseMessage response;

            try
            {
                response = await client.PutAsJsonAsync(
                    $"api/InterviewCategories/admin/{id}",
                    new { model.Name, model.Description, model.IsActive });
            }
            catch (HttpRequestException)
            {
                ModelState.AddModelError(string.Empty,
                    "The interview API is unavailable. Please try again.");
                return View(model);
            }
            catch (TaskCanceledException)
            {
                ModelState.AddModelError(string.Empty,
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
                    TempData["HighlightCategoryId"] = id;
                    TempData["HighlightType"] = "updated";
                    return RedirectToAction(nameof(Index));
                }

                if (response.StatusCode == HttpStatusCode.Conflict)
                {
                    ModelState.AddModelError(nameof(model.Name),
                        "Another category already uses this name.");
                    return View(model);
                }

                if (response.StatusCode == HttpStatusCode.BadRequest)
                {
                    ModelState.AddModelError(string.Empty,
                        "Please check the category details and try again.");
                    return View(model);
                }

                ModelState.AddModelError(string.Empty,
                    "Unable to update the interview category. Please try again.");
                return View(model);
            }
        }
    }
}
