
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using TsebeCareerPrep.Mvc.Models;

namespace TsebeCareerPrep.Mvc.Controllers
{
    [Authorize(Roles = "Administrator")]
    public class AdminMisconceptionRulesController : Controller
    {
        private readonly IHttpClientFactory _factory;

        public AdminMisconceptionRulesController(
            IHttpClientFactory factory)
        {
            _factory = factory;
        }

        private HttpClient Api() =>
            _factory.CreateClient("AuthenticatedInterviewApi");

        private IActionResult? AuthError(HttpStatusCode status)
        {
            if (status == HttpStatusCode.Unauthorized)
                return Challenge();

            if (status == HttpStatusCode.Forbidden)
                return Forbid();

            return null;
        }

        [HttpGet]
        public async Task<IActionResult> Index(int id)
        {
            if (id <= 0)
                return BadRequest("Invalid question ID.");

            var client = Api();

            try
            {
                using var questionsResponse = await client.GetAsync(
                    "api/InterviewQuestions/admin/all");

                var auth = AuthError(questionsResponse.StatusCode);
                if (auth != null) return auth;

                if (!questionsResponse.IsSuccessStatusCode)
                    return StatusCode(
                        (int)questionsResponse.StatusCode);

                var questions = await questionsResponse.Content
                    .ReadFromJsonAsync<List<InterviewQuestionViewModel>>()
                    ?? new();

                var question = questions.FirstOrDefault(q => q.Id == id);

                if (question == null)
                    return NotFound("Question not found.");

                using var rulesResponse = await client.GetAsync(
                    $"api/admin/misconception-rules/question/{id}");

                auth = AuthError(rulesResponse.StatusCode);
                if (auth != null) return auth;

                if (!rulesResponse.IsSuccessStatusCode)
                    return StatusCode((int)rulesResponse.StatusCode);

                var rules = await rulesResponse.Content
                    .ReadFromJsonAsync<List<AdminMisconceptionRuleItem>>()
                    ?? new();

                return View(new AdminMisconceptionRulesViewModel
                {
                    QuestionId = id,
                    QuestionText = question.QuestionText,
                    CategoryId = question.InterviewCategoryId,
                    Rules = rules
                });
            }
            catch (HttpRequestException)
            {
                return StatusCode(503, "Interview API unavailable.");
            }
            catch (TaskCanceledException)
            {
                return StatusCode(504, "Interview API timed out.");
            }
            catch (JsonException)
            {
                return StatusCode(502, "Invalid API response.");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            AdminMisconceptionRuleInput model)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] =
                    "Please enter a valid phrase and feedback message.";

                return RedirectToAction(
                    nameof(Index),
                    new { id = model.InterviewQuestionId });
            }

            var client = Api();

            try
            {
                using var response = await client.PostAsJsonAsync(
                    "api/admin/misconception-rules", model);

                var auth = AuthError(response.StatusCode);
                if (auth != null) return auth;

                if (response.IsSuccessStatusCode)
                {
                    TempData["Success"] =
                        "Misconception rule created successfully.";

                    using var json = await JsonDocument.ParseAsync(
                        await response.Content.ReadAsStreamAsync());

                    if (json.RootElement.TryGetProperty("id", out var id))
                    {
                        TempData["HighlightRuleId"] = id.GetInt32();
                        TempData["HighlightType"] = "created";
                    }
                }

                else if (response.StatusCode == HttpStatusCode.Conflict)
                    TempData["Error"] =
                        "This trigger phrase already exists for the question.";
                else
                    TempData["Error"] =
                        "Unable to create the misconception rule.";
            }
            catch (HttpRequestException)
            {
                TempData["Error"] = "Interview API unavailable.";
            }
            catch (TaskCanceledException)
            {
                TempData["Error"] = "Interview API timed out.";
            }

            return RedirectToAction(
                nameof(Index),
                new { id = model.InterviewQuestionId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(
            int id,
            AdminMisconceptionRuleInput model)
        {
            if (id <= 0 || !ModelState.IsValid)
            {
                TempData["Error"] = "Invalid rule details.";
                return RedirectToAction(
                    nameof(Index),
                    new { id = model.InterviewQuestionId });
            }

            var client = Api();

            try
            {
                using var response = await client.PutAsJsonAsync(
                    $"api/admin/misconception-rules/{id}", model);

                var auth = AuthError(response.StatusCode);
                if (auth != null) return auth;

                TempData[response.IsSuccessStatusCode
                    ? "Success" : "Error"] =
                    response.IsSuccessStatusCode
                        ? "Misconception rule updated successfully."
                        : "Unable to update the rule. Check for duplicate phrases.";

                if (response.IsSuccessStatusCode)
                {
                    TempData["HighlightRuleId"] = id;
                    TempData["HighlightType"] = "updated";
                }
            }
            catch (HttpRequestException)
            {
                TempData["Error"] = "Interview API unavailable.";
            }
            catch (TaskCanceledException)
            {
                TempData["Error"] = "Interview API timed out.";
            }

            return RedirectToAction(
                nameof(Index),
                new { id = model.InterviewQuestionId });
        }
    }
}
