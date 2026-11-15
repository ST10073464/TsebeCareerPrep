
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using TsebeCareerPrep.Mvc.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using TsebeCareerPrep.Mvc.Services;

namespace TsebeCareerPrep.Mvc.Controllers
{
    [Authorize(Roles = "Administrator")]
    public class AdminQuestionsController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly InterviewCategoryService _categoryService;

        public AdminQuestionsController(
            IHttpClientFactory httpClientFactory,
            InterviewCategoryService categoryService)
        {
            _httpClientFactory = httpClientFactory;
            _categoryService = categoryService;
        }


        // =====================================================
        // GET: /AdminQuestions/ByCategory/1
        // Display questions belonging to one category.
        // =====================================================
        [HttpGet]
        public async Task<IActionResult> ByCategory(int id)
        {
            if (id <= 0)
            {
                return BadRequest("Invalid category ID.");
            }

            var client = _httpClientFactory.CreateClient(
                "AuthenticatedInterviewApi");

            try
            {
                using var categoryResponse = await client.GetAsync(
                    "api/InterviewCategories/admin/all");

                if (categoryResponse.StatusCode ==
                    HttpStatusCode.Unauthorized)
                    return Challenge();

                if (categoryResponse.StatusCode ==
                    HttpStatusCode.Forbidden)
                    return Forbid();

                if (!categoryResponse.IsSuccessStatusCode)
                {
                    return StatusCode(
                        (int)categoryResponse.StatusCode,
                        "Unable to retrieve interview categories.");
                }

                var categories = await categoryResponse.Content
                    .ReadFromJsonAsync<
                        List<InterviewCategoryAdminViewModel>>()
                    ?? new List<InterviewCategoryAdminViewModel>();

                var category = categories.FirstOrDefault(
                    c => c.Id == id);

                if (category == null)
                {
                    return NotFound("Interview category not found.");
                }

                using var questionResponse = await client.GetAsync(
                    "api/InterviewQuestions/admin/all");

                if (questionResponse.StatusCode ==
                    HttpStatusCode.Unauthorized)
                    return Challenge();

                if (questionResponse.StatusCode ==
                    HttpStatusCode.Forbidden)
                    return Forbid();

                if (!questionResponse.IsSuccessStatusCode)
                {
                    return StatusCode(
                        (int)questionResponse.StatusCode,
                        "Unable to retrieve interview questions.");
                }

                var questions = await questionResponse.Content
                    .ReadFromJsonAsync<
                        List<InterviewQuestionViewModel>>()
                    ?? new List<InterviewQuestionViewModel>();

                var categoryQuestions = questions
                    .Where(q => q.InterviewCategoryId == id)
                    .OrderBy(q => q.Id)
                    .ToList();

                foreach (var question in categoryQuestions)
                {
                    question.InterviewCategoryName = category.Name;
                }

                ViewBag.CategoryId = category.Id;
                ViewBag.CategoryName = category.Name;
                ViewBag.CategoryDescription = category.Description;
                ViewBag.CategoryIsActive = category.IsActive;

                return View("ByCategory", categoryQuestions);
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
            catch (JsonException)
            {
                return StatusCode(
                    502,
                    "The interview API returned an invalid response.");
            }
        }


        // =====================================================
        // GET: /AdminQuestions
        // Display all questions, including inactive questions.
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
                    "api/InterviewQuestions/admin/all");
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
                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    return Challenge();
                }

                if (response.StatusCode == HttpStatusCode.Forbidden)
                {
                    return Forbid();
                }

                if (!response.IsSuccessStatusCode)
                {
                    return StatusCode(
                        (int)response.StatusCode,
                        "Unable to retrieve interview questions.");
                }


                var questions = await response.Content
                    .ReadFromJsonAsync<
                        List<InterviewQuestionViewModel>>()
                    ?? new List<InterviewQuestionViewModel>();

                // Retrieve the interview categories from the API.
                var categories =
                    await _categoryService.GetCategoriesAsync();

                // Match each question to its category name.
                var categoryLookup = categories.ToDictionary(
                    category => category.Id,
                    category => category.Name);

                foreach (var question in questions)
                {
                    question.InterviewCategoryName =
                        categoryLookup.TryGetValue(
                            question.InterviewCategoryId,
                            out var categoryName)
                        ? categoryName
                        : $"Unknown Category ({question.InterviewCategoryId})";
                }

                return View(questions);

            }
        }

        // =====================================================
        // GET: /AdminQuestions/Create
        // Display the Create Interview Question form.
        // =====================================================


        [HttpGet]
        public async Task<IActionResult> Create(int? categoryId)
        {
            // Only accept a positive category ID from the URL.
            int? selectedCategoryId =
                categoryId.HasValue && categoryId.Value > 0
                    ? categoryId.Value
                    : null;

            // Load categories with the appropriate selection.
            await LoadCategoriesAsync(selectedCategoryId);

            var model = new CreateInterviewQuestionViewModel
            {
                // Zero represents no category selected.
                InterviewCategoryId = selectedCategoryId ?? 0
            };

            // Prevent a previous category selection from
            // overriding the model on this GET request.
            ModelState.Remove(
                nameof(CreateInterviewQuestionViewModel.InterviewCategoryId));

            return View(model);
        }



        // =====================================================
        // POST: /AdminQuestions/Create
        // Validate and create an interview question.
        // =====================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            CreateInterviewQuestionViewModel model)
        {

            await LoadCategoriesAsync(model.InterviewCategoryId);

            // Verify the submitted category against the available
            // interview categories before sending it to the API.
            var categories = await _categoryService.GetCategoriesAsync();

            var selectedCategory = categories.FirstOrDefault(
                category =>
                    category.Id == model.InterviewCategoryId &&
                    category.IsActive);

            if (selectedCategory == null)
            {
                ModelState.AddModelError(
                    nameof(model.InterviewCategoryId),
                    "Please select a valid, active interview category.");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var allowedDifficulties = new[]
            {
                "Beginner",
                "Intermediate",
                "Advanced"
            };

            var difficulty = model.DifficultyLevel?.Trim();

            if (!allowedDifficulties.Contains(
                difficulty,
                StringComparer.OrdinalIgnoreCase))
            {
                ModelState.AddModelError(
                    nameof(model.DifficultyLevel),
                    "Please select a valid difficulty level.");

                return View(model);
            }

            model.DifficultyLevel = allowedDifficulties
                .First(value => value.Equals(
                    difficulty,
                    StringComparison.OrdinalIgnoreCase));

            model.QuestionText = model.QuestionText.Trim();
            model.SuggestedAnswer = model.SuggestedAnswer?.Trim();

            var client = _httpClientFactory.CreateClient(
                "AuthenticatedInterviewApi");

            HttpResponseMessage response;

            try
            {
                response = await client.PostAsJsonAsync(
                    "api/InterviewQuestions/admin",
                    model);
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
                    int? newQuestionId = null;

                    try
                    {
                        using var document = JsonDocument.Parse(
                            await response.Content.ReadAsStringAsync());

                        if (document.RootElement.TryGetProperty(
                                "id", out var idProperty) &&
                            idProperty.ValueKind == JsonValueKind.Number &&
                            idProperty.TryGetInt32(out var createdId))
                        {
                            newQuestionId = createdId;
                        }
                    }
                    catch (JsonException)
                    {
                        // The API has already created the question.
                        // Continue without highlighting if its ID is unreadable.
                    }

                    TempData["SuccessMessage"] =
                        $"Interview question created successfully in {selectedCategory!.Name}.";

                    if (newQuestionId.HasValue)
                    {
                        TempData["NewQuestionId"] = newQuestionId.Value;
                        TempData["HighlightQuestionId"] = newQuestionId.Value;
                        TempData["HighlightType"] = "created";
                    }

                    return RedirectToAction(
                        nameof(ByCategory),
                        new { id = model.InterviewCategoryId });
                }

                if (response.StatusCode == HttpStatusCode.BadRequest)
                {
                    await AddApiValidationErrorAsync(
                        response,
                        "Please check the question details and try again.");

                    return View(model);
                }

                ModelState.AddModelError(
                    string.Empty,
                    "Unable to create the interview question. Please try again.");

                return View(model);
            }
        }

        // =====================================================
        // GET: /AdminQuestions/Edit/10
        // Retrieve an existing question for editing.
        // =====================================================
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            if (id <= 0)
                return BadRequest("Invalid question ID.");

            var client = _httpClientFactory.CreateClient(
                "AuthenticatedInterviewApi");

            HttpResponseMessage response;

            try
            {
                // The Administrator endpoint includes inactive
                // questions and their suggested answers.
                response = await client.GetAsync(
                    "api/InterviewQuestions/admin/all");
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
                if (response.StatusCode == HttpStatusCode.Unauthorized)
                    return Challenge();

                if (response.StatusCode == HttpStatusCode.Forbidden)
                    return Forbid();

                if (!response.IsSuccessStatusCode)
                {
                    return StatusCode(
                        (int)response.StatusCode,
                        "Unable to retrieve the interview question.");
                }

                List<InterviewQuestionViewModel>? questions;

                try
                {
                    questions = await response.Content
                        .ReadFromJsonAsync<
                            List<InterviewQuestionViewModel>>();
                }
                catch (JsonException)
                {
                    return StatusCode(
                        502,
                        "The interview API returned an invalid response.");
                }

                var question = questions?
                    .FirstOrDefault(q => q.Id == id);

                if (question == null)
                    return NotFound("Interview question not found.");


                var model = new EditInterviewQuestionViewModel
                {
                    Id = question.Id,
                    InterviewCategoryId = question.InterviewCategoryId,
                    QuestionText = question.QuestionText,
                    SuggestedAnswer = question.SuggestedAnswer,
                    DifficultyLevel = question.DifficultyLevel,
                    IsActive = question.IsActive
                };

                // Load the current interview categories dynamically.
                // Include the question's existing category even if inactive.
                await LoadCategoriesAsync(model.InterviewCategoryId);

                return View(model);

            }
        }

        // =====================================================
        // POST: /AdminQuestions/Edit/10
        // Update a question without allowing category transfers.
        // =====================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            EditInterviewQuestionViewModel model)
        {
            if (id <= 0 || id != model.Id)
                return BadRequest("Invalid question ID.");

            var client = _httpClientFactory.CreateClient(
                "AuthenticatedInterviewApi");

            // Always obtain the category from the existing question,
            // rather than trusting the category ID submitted by the browser.
            try
            {
                using var lookupResponse = await client.GetAsync(
                    "api/InterviewQuestions/admin/all");

                if (lookupResponse.StatusCode == HttpStatusCode.Unauthorized)
                    return Challenge();

                if (lookupResponse.StatusCode == HttpStatusCode.Forbidden)
                    return Forbid();

                if (!lookupResponse.IsSuccessStatusCode)
                    return StatusCode(
                        (int)lookupResponse.StatusCode,
                        "Unable to verify the question's original category.");

                var questions = await lookupResponse.Content
                    .ReadFromJsonAsync<List<InterviewQuestionViewModel>>()
                    ?? new List<InterviewQuestionViewModel>();

                var existingQuestion = questions.FirstOrDefault(q => q.Id == id);
                if (existingQuestion == null)
                    return NotFound("Interview question not found.");

                int originalCategoryId = existingQuestion.InterviewCategoryId;
                await LoadCategoriesAsync(originalCategoryId);

                if (model.InterviewCategoryId != originalCategoryId)
                {
                    // Restore the correct category when redisplaying the form.
                    model.InterviewCategoryId = originalCategoryId;
                    ModelState.Remove(nameof(model.InterviewCategoryId));
                    ModelState.AddModelError(
                        nameof(model.InterviewCategoryId),
                        "The interview category is locked. A question cannot be moved to another category.");
                    return View(model);
                }

                if (!ModelState.IsValid)
                    return View(model);

                var allowedDifficulties = new[]
                {
                    "Beginner", "Intermediate", "Advanced"
                };

                var difficulty = model.DifficultyLevel?.Trim();
                if (!allowedDifficulties.Contains(
                    difficulty, StringComparer.OrdinalIgnoreCase))
                {
                    ModelState.AddModelError(
                        nameof(model.DifficultyLevel),
                        "Please select a valid difficulty level.");
                    return View(model);
                }

                model.DifficultyLevel = allowedDifficulties.First(value =>
                    value.Equals(difficulty, StringComparison.OrdinalIgnoreCase));
                model.QuestionText = model.QuestionText?.Trim() ?? string.Empty;
                model.SuggestedAnswer = model.SuggestedAnswer?.Trim();
                model.InterviewCategoryId = originalCategoryId;

                using var response = await client.PutAsJsonAsync(
                    $"api/InterviewQuestions/admin/{id}", model);

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                    return Challenge();

                if (response.StatusCode == HttpStatusCode.Forbidden)
                    return Forbid();

                if (response.StatusCode == HttpStatusCode.NotFound)
                    return NotFound("Interview question not found.");

                if (response.IsSuccessStatusCode)
                {
                    TempData["HighlightQuestionId"] = model.Id;
                    TempData["HighlightType"] = "updated";
                    TempData["SuccessMessage"] =
                        $"Interview question #{model.Id} updated successfully.";

                    return RedirectToAction(
                        nameof(ByCategory),
                        new { id = originalCategoryId });
                }

                if (response.StatusCode == HttpStatusCode.BadRequest)
                {
                    await AddApiValidationErrorAsync(
                        response,
                        "Please check the question details and try again.");
                    return View(model);
                }

                ModelState.AddModelError(
                    string.Empty,
                    "Unable to update the interview question. Please try again.");
                return View(model);
            }
            catch (HttpRequestException)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "The interview API is unavailable. Please try again.");
                await LoadCategoriesAsync(model.InterviewCategoryId);
                return View(model);
            }
            catch (TaskCanceledException)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "The request timed out. Please try again.");
                await LoadCategoriesAsync(model.InterviewCategoryId);
                return View(model);
            }
            catch (JsonException)
            {
                return StatusCode(
                    502, "The interview API returned an invalid response.");
            }
        }

        // =====================================================
        // Read validation messages returned by the API.
        // =====================================================
        private async Task AddApiValidationErrorAsync(
            HttpResponseMessage response,
            string defaultMessage)
        {
            var errorMessage = defaultMessage;

            try
            {
                var errorJson = await response.Content
                    .ReadAsStringAsync();

                using var document = JsonDocument.Parse(errorJson);

                if (document.RootElement.TryGetProperty(
                    "message",
                    out var messageProperty) &&
                    messageProperty.ValueKind == JsonValueKind.String)
                {
                    errorMessage =
                        messageProperty.GetString() ?? defaultMessage;
                }
                else if (document.RootElement.TryGetProperty(
                    "title",
                    out var titleProperty) &&
                    titleProperty.ValueKind == JsonValueKind.String)
                {
                    errorMessage =
                        titleProperty.GetString() ?? defaultMessage;
                }
            }
            catch (JsonException)
            {
                // Keep the default message if the API response
                // does not contain valid JSON.
            }

            ModelState.AddModelError(string.Empty, errorMessage);
        }

        // =====================================================
        // Load interview categories dynamically for dropdowns.
        // Include the currently selected inactive category
        // when editing an existing question.
        // =====================================================
        private async Task LoadCategoriesAsync(
            int? selectedCategoryId = null)
        {
            var categories =
                await _categoryService.GetCategoriesAsync();

            ViewBag.Categories = categories
                .Where(category =>
                    category.IsActive ||
                    category.Id == selectedCategoryId)
                .OrderBy(category => category.Name)
                .Select(category => new SelectListItem
                {
                    Value = category.Id.ToString(),
                    Text = category.IsActive
                        ? category.Name
                        : $"{category.Name} (Inactive)",
                    Selected = category.Id == selectedCategoryId
                })

                .ToList();
        }
    }
}
