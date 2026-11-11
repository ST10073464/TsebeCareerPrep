using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Json;
using TsebeCareerPrep.Mvc.Models;
using System.Net;

namespace TsebeCareerPrep.Mvc.Controllers
{
    public class InterviewSessionsController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public InterviewSessionsController(
            IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        // POST: /InterviewSessions/Start
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Start(int categoryId)
        {
            if (categoryId <= 0)
            {
                return BadRequest(
                    "Please select a valid interview category.");
            }

            var client = _httpClientFactory.CreateClient();

            client.BaseAddress =
                new Uri("https://localhost:7101/");

            var response = await client.PostAsJsonAsync(
                "api/InterviewSessions/start",
                new
                {
                    InterviewCategoryId = categoryId
                });

            if (!response.IsSuccessStatusCode)
            {
                TempData["Error"] =
                    "Unable to start the interview. Please try again.";

                return RedirectToAction(
                    "Index", "InterviewCategories");
            }

            var session = await response.Content
                .ReadFromJsonAsync<InterviewSessionViewModel>();

            if (session == null)
            {
                return StatusCode(502,
                    "The interview API returned an invalid response.");
            }

            return RedirectToAction(
                "Details", new { id = session.Id });
        }

        // GET: /InterviewSessions/Details/1
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            if (id <= 0)
                return BadRequest();

            var client = _httpClientFactory.CreateClient();

            client.BaseAddress =
                new Uri("https://localhost:7101/");

            var response = await client.GetAsync(
                $"api/InterviewSessions/{id}");

            if (response.StatusCode ==
                System.Net.HttpStatusCode.NotFound)
            {
                return NotFound();
            }

            response.EnsureSuccessStatusCode();

            var session = await response.Content
                .ReadFromJsonAsync<InterviewSessionViewModel>();

            if (session == null)
                return NotFound();

            return View(session);
        }


        // GET: /InterviewSessions/Answer?sessionId=3&questionId=1
        [HttpGet]
        public async Task<IActionResult> Answer(
            int sessionId,
            int questionId)
        {
            if (sessionId <= 0 || questionId <= 0)
                return BadRequest();

            var client = _httpClientFactory.CreateClient();
            client.BaseAddress = new Uri("https://localhost:7101/");

            var sessionResponse = await client.GetAsync(
                $"api/InterviewSessions/{sessionId}");

            if (sessionResponse.StatusCode == HttpStatusCode.NotFound)
                return NotFound("Interview session not found.");

            sessionResponse.EnsureSuccessStatusCode();

            var session = await sessionResponse.Content
                .ReadFromJsonAsync<InterviewSessionViewModel>();

            if (session == null)
                return StatusCode(502, "Invalid session response.");

            if (session.Status != "InProgress")
                return BadRequest("This interview session is not active.");

            var questionResponse = await client.GetAsync(
                $"api/InterviewQuestions/{questionId}");

            if (questionResponse.StatusCode == HttpStatusCode.NotFound)
                return NotFound("Interview question not found.");

            questionResponse.EnsureSuccessStatusCode();

            var question = await questionResponse.Content
                .ReadFromJsonAsync<InterviewAnswerQuestionDto>();

            if (question == null)
                return StatusCode(502, "Invalid question response.");

            if (question.InterviewCategoryId != session.InterviewCategoryId)
                return BadRequest("Question does not belong to this interview category.");

            var model = new SubmitInterviewAnswerViewModel
            {
                InterviewSessionId = sessionId,
                InterviewQuestionId = questionId,
                QuestionText = question.QuestionText
            };

            return View(model);
        }

        // POST: /InterviewSessions/Answer
        [HttpPost]
        [ValidateAntiForgeryToken]
        [ActionName("Answer")]
        public async Task<IActionResult> SubmitAnswer(
            SubmitInterviewAnswerViewModel model)
        {
            // QuestionText is display-only and will be loaded again.
            ModelState.Remove(nameof(model.QuestionText));

            if (!ModelState.IsValid)
            {
                return await RedisplayAnswer(model);
            }

            var client = _httpClientFactory.CreateClient();
            client.BaseAddress = new Uri("https://localhost:7101/");

            var response = await client.PostAsJsonAsync(
                "api/InterviewResponses/submit",
                new
                {
                    model.InterviewSessionId,
                    model.InterviewQuestionId,
                    model.AnswerText
                });

            if (response.IsSuccessStatusCode)
            {
                TempData["Success"] = "Your answer was submitted successfully.";

                return RedirectToAction(
                    "Details",
                    new { id = model.InterviewSessionId });
            }

            if (response.StatusCode == HttpStatusCode.Conflict)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "You have already answered this question.");
            }
            else
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Unable to submit your answer. Please check that the session is active and the question is valid.");
            }

            return await RedisplayAnswer(model);
        }

        private async Task<IActionResult> RedisplayAnswer(
            SubmitInterviewAnswerViewModel model)
        {
            var client = _httpClientFactory.CreateClient();
            client.BaseAddress = new Uri("https://localhost:7101/");

            var response = await client.GetAsync(
                $"api/InterviewQuestions/{model.InterviewQuestionId}");

            if (response.IsSuccessStatusCode)
            {
                var question = await response.Content
                    .ReadFromJsonAsync<InterviewAnswerQuestionDto>();

                model.QuestionText = question?.QuestionText ?? string.Empty;
            }

            return View("Answer", model);
        }

        private sealed class InterviewAnswerQuestionDto
        {
            public int Id { get; set; }
            public int InterviewCategoryId { get; set; }
            public string QuestionText { get; set; } = string.Empty;
        }


        // GET: /InterviewSessions/Questions/3
        [HttpGet]
        public async Task<IActionResult> Questions(int id)
        {
            if (id <= 0)
                return BadRequest("Invalid interview session ID.");

            var client = _httpClientFactory.CreateClient();
            client.BaseAddress = new Uri("https://localhost:7101/");

            // Retrieve the active interview session.
            var sessionResponse = await client.GetAsync(
                $"api/InterviewSessions/{id}");

            if (sessionResponse.StatusCode ==
                System.Net.HttpStatusCode.NotFound)
            {
                return NotFound("Interview session not found.");
            }

            sessionResponse.EnsureSuccessStatusCode();

            var session = await sessionResponse.Content
                .ReadFromJsonAsync<InterviewSessionViewModel>();

            if (session == null)
                return StatusCode(502, "Invalid session response.");

            // Retrieve questions for this session's category.
            var questionsResponse = await client.GetAsync(
                $"api/InterviewQuestions/category/{session.InterviewCategoryId}");

            questionsResponse.EnsureSuccessStatusCode();

            var questions = await questionsResponse.Content
                .ReadFromJsonAsync<List<InterviewSessionQuestionViewModel>>()
                ?? new List<InterviewSessionQuestionViewModel>();


            // Retrieve answers already submitted for this session.
            var answersResponse = await client.GetAsync(
                $"api/InterviewResponses/session/{session.Id}");

            answersResponse.EnsureSuccessStatusCode();

            var answeredQuestions = await answersResponse.Content
                .ReadFromJsonAsync<List<SessionAnsweredQuestionDto>>()
                ?? new List<SessionAnsweredQuestionDto>();

            // Use a HashSet for efficient question ID lookups.
            var answeredQuestionIds = answeredQuestions
                .Select(r => r.InterviewQuestionId)
                .ToHashSet();

            // Mark each question as answered or unanswered.
            foreach (var question in questions)
            {
                question.IsAnswered = answeredQuestionIds.Contains(question.Id);
            }


            var model = new InterviewSessionQuestionsViewModel
            {
                SessionId = session.Id,
                InterviewCategoryId = session.InterviewCategoryId,
                CategoryName = session.CategoryName,
                Status = session.Status,
                Questions = questions
            };

            return View(model);
        }



        // Response metadata returned by the session responses API.
        private sealed class SessionAnsweredQuestionDto
        {
            public int InterviewQuestionId { get; set; }
        }

        // POST: /InterviewSessions/Complete/3
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Complete(int id)
        {
            if (id <= 0)
            {
                return BadRequest("Invalid interview session ID.");
            }

            var client = _httpClientFactory.CreateClient();
            client.BaseAddress = new Uri("https://localhost:7101/");

            var response = await client.PostAsync(
                $"api/InterviewSessions/{id}/complete",
                null);

            if (response.IsSuccessStatusCode)
            {
                TempData["Success"] =
                    "Your interview has been completed successfully.";

                return RedirectToAction("Details", new { id });
            }

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return NotFound("Interview session not found.");
            }

            if (response.StatusCode == HttpStatusCode.Conflict)
            {
                TempData["Error"] =
                    "This interview session has already been completed.";
            }
            else if (response.StatusCode == HttpStatusCode.BadRequest)
            {
                TempData["Error"] =
                    "You must submit at least one answer before completing the interview.";
            }
            else
            {
                TempData["Error"] =
                    "Unable to complete the interview. Please try again.";
            }

            return RedirectToAction("Details", new { id });
        }

        // GET: /InterviewSessions/Results/3
        [HttpGet]
        public async Task<IActionResult> Results(int id)
        {
            if (id <= 0)
            {
                return BadRequest("Invalid interview session ID.");
            }

            var client = _httpClientFactory.CreateClient();
            client.BaseAddress = new Uri("https://localhost:7101/");

            var response = await client.GetAsync(
                $"api/InterviewSessions/{id}/results");

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return NotFound("Interview session not found.");
            }

            if (response.StatusCode == HttpStatusCode.BadRequest)
            {
                TempData["Error"] =
                    "Interview results are available only after completing the session.";

                return RedirectToAction("Details", new { id });
            }

            response.EnsureSuccessStatusCode();

            var model = await response.Content
                .ReadFromJsonAsync<InterviewResultsViewModel>();

            if (model == null)
            {
                return StatusCode(
                    502,
                    "The interview API returned an invalid results response.");
            }

            return View(model);
        }


        // GET: /InterviewSessions/History
        [HttpGet]
        public async Task<IActionResult> History()
        {
            var client = _httpClientFactory.CreateClient();

            client.BaseAddress =
                new Uri("https://localhost:7101/");

            var response = await client.GetAsync(
                "api/InterviewSessions/history");

            response.EnsureSuccessStatusCode();

            var sessions = await response.Content
                .ReadFromJsonAsync<List<InterviewHistoryViewModel>>()
                ?? new List<InterviewHistoryViewModel>();

            return View(sessions);
        }


        // GET: /InterviewSessions/Progress
        [HttpGet]
        public async Task<IActionResult> Progress()
        {
            var client = _httpClientFactory.CreateClient();

            client.BaseAddress =
                new Uri("https://localhost:7101/");

            var response = await client.GetAsync(
                "api/InterviewSessions/progress");

            if (!response.IsSuccessStatusCode)
            {
                return StatusCode(
                    502,
                    "Unable to retrieve interview progress. Please try again.");
            }

            var model = await response.Content
                .ReadFromJsonAsync<InterviewProgressViewModel>();

            if (model == null)
            {
                return StatusCode(
                    502,
                    "The interview API returned an invalid progress response.");
            }

            return View(model);
        }




    }
}
