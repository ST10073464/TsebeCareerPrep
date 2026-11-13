
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using TsebeCareerPrep.Mvc.Models;

namespace TsebeCareerPrep.Mvc.Controllers
{
    [Authorize(Roles = "Administrator")]
    public class AdminQuestionsController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public AdminQuestionsController(
            IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
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
                        List<InterviewQuestionViewModel>>();

                return View(questions ??
                    new List<InterviewQuestionViewModel>());
            }
        }

        // =====================================================
        // GET: /AdminQuestions/Create
        // Display the Create Interview Question form.
        // =====================================================
        [HttpGet]
        public IActionResult Create()
        {
            return View(
                new CreateInterviewQuestionViewModel());
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
                    TempData["SuccessMessage"] =
                        "Interview question created successfully.";

                    return RedirectToAction(nameof(Index));
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

                return View(model);
            }
        }

        // =====================================================
        // POST: /AdminQuestions/Edit/10
        // Validate and update an existing question.
        // =====================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            EditInterviewQuestionViewModel model)
        {
            if (id <= 0 || id != model.Id)
                return BadRequest("Invalid question ID.");

            if (!ModelState.IsValid)
                return View(model);

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
                response = await client.PutAsJsonAsync(
                    $"api/InterviewQuestions/admin/{id}",
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

                if (response.StatusCode == HttpStatusCode.NotFound)
                    return NotFound("Interview question not found.");

                if (response.IsSuccessStatusCode)
                {
                    TempData["SuccessMessage"] =
                        "Interview question updated successfully.";

                    return RedirectToAction(nameof(Index));
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
    }
}
