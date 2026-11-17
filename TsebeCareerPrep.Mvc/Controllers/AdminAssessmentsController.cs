
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Net.Http.Json;
using TsebeCareerPrep.Mvc.Models;

namespace TsebeCareerPrep.Mvc.Controllers
{

    [Authorize(Roles = "Administrator,SuperAdministrator")]

    public class AdminAssessmentsController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public AdminAssessmentsController(
            IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            if (id <= 0)
                return BadRequest();

            var client = _httpClientFactory.CreateClient(
                "AuthenticatedInterviewApi");

            try
            {
                using var questionsResponse = await client.GetAsync(
                    "api/InterviewQuestions/admin/all");

                if (questionsResponse.StatusCode == HttpStatusCode.Unauthorized)
                    return Challenge();

                if (questionsResponse.StatusCode == HttpStatusCode.Forbidden)
                    return Forbid();

                if (!questionsResponse.IsSuccessStatusCode)
                    return StatusCode((int)questionsResponse.StatusCode);

                var questions = await questionsResponse.Content
                    .ReadFromJsonAsync<List<InterviewQuestionViewModel>>()
                    ?? new();

                var question = questions.FirstOrDefault(q => q.Id == id);

                if (question == null)
                    return NotFound("Question not found.");

                var model = new AdminAssessmentRubricViewModel
                {
                    InterviewQuestionId = question.Id,
                    QuestionText = question.QuestionText,
                    CategoryId = question.InterviewCategoryId
                };

                using var rubricResponse = await client.GetAsync(
                    $"api/admin/assessment-rubrics/question/{id}");

                if (rubricResponse.StatusCode == HttpStatusCode.Unauthorized)
                    return Challenge();

                if (rubricResponse.StatusCode == HttpStatusCode.Forbidden)
                    return Forbid();

                if (rubricResponse.StatusCode == HttpStatusCode.NotFound)
                    return View(model);

                if (!rubricResponse.IsSuccessStatusCode)
                    return StatusCode((int)rubricResponse.StatusCode);

                var existing = await rubricResponse.Content
                    .ReadFromJsonAsync<AdminAssessmentRubricViewModel>();

                if (existing != null)
                {
                    existing.QuestionText = question.QuestionText;
                    existing.CategoryId = question.InterviewCategoryId;
                    existing.InterviewQuestionId = question.Id;
                    model = existing;
                }

                return View(model);
            }
            catch (HttpRequestException)
            {
                return StatusCode(503, "Interview API unavailable.");
            }
            catch (TaskCanceledException)
            {
                return StatusCode(504, "Interview API request timed out.");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            AdminAssessmentRubricViewModel model)
        {
            if (id <= 0 || id != model.InterviewQuestionId)
                return BadRequest();

            int totalMarks =
                model.TechnicalAccuracyMarks +
                model.KeyConceptMarks +
                model.ClarityMarks +
                model.PracticalExampleMarks;

            if (totalMarks != 10)
                ModelState.AddModelError(
                    string.Empty,
                    "Assessment criteria must total 10 marks.");

            if (!ModelState.IsValid)
                return View(model);

            var client = _httpClientFactory.CreateClient(
                "AuthenticatedInterviewApi");

            try
            {
                using var response = await client.PutAsJsonAsync(
                    $"api/admin/assessment-rubrics/question/{id}",
                    model);

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                    return Challenge();

                if (response.StatusCode == HttpStatusCode.Forbidden)
                    return Forbid();

                if (response.IsSuccessStatusCode)
                {
                    TempData["SuccessMessage"] =
                        "Assessment rubric saved successfully.";

                    TempData["HighlightRubric"] = true;
                    TempData["HighlightType"] = "updated";

                    return RedirectToAction(nameof(Edit), new { id });
                }

                ModelState.AddModelError(
                    string.Empty,
                    response.StatusCode == HttpStatusCode.BadRequest
                        ? "Please check the rubric details."
                        : "Unable to save the rubric.");
            }
            catch (HttpRequestException)
            {
                ModelState.AddModelError(
                    string.Empty, "Interview API unavailable.");
            }
            catch (TaskCanceledException)
            {
                ModelState.AddModelError(
                    string.Empty, "Interview API request timed out.");
            }

            return View(model);
        }
    }
}
