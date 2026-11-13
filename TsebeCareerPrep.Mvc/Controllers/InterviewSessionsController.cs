using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Net.Http.Json;
using TsebeCareerPrep.Mvc.Models;
using System.Net;

namespace TsebeCareerPrep.Mvc.Controllers
{
    [Authorize]
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

            var client = _httpClientFactory.CreateClient("AuthenticatedInterviewApi");


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
                "Ready", new { id = session.Id });

        }


        // POST: /InterviewSessions/Restart/41
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Restart(int id)
        {
            if (id <= 0)
            {
                return BadRequest(
                    "Invalid interview session ID.");
            }

            var client = _httpClientFactory.CreateClient(
                "AuthenticatedInterviewApi");

            var response = await client.PostAsync(
                $"api/InterviewSessions/{id}/restart",
                null);

            if (response.IsSuccessStatusCode)
            {
                TempData["Success"] =
                    "Your interview has been restarted. " +
                    "You can begin again from Question 1.";

                return RedirectToAction(
                    "Ready", new { id });
            }

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return NotFound(
                    "Interview session not found.");
            }

            if (response.StatusCode == HttpStatusCode.Conflict)
            {
                TempData["Error"] =
                    "This interview cannot be restarted " +
                    "because it is no longer in progress.";
            }
            else
            {
                TempData["Error"] =
                    "Unable to restart your interview. " +
                    "Please try again.";
            }

            return RedirectToAction(
                "Options", new { id });
        }


        // GET: /InterviewSessions/Ready/41
        [HttpGet]
        public async Task<IActionResult> Ready(int id)
        {
            if (id <= 0)
            {
                return BadRequest(
                    "Invalid interview session ID.");
            }

            var client = _httpClientFactory.CreateClient(
                "AuthenticatedInterviewApi");

            var response = await client.GetAsync(
                $"api/InterviewSessions/{id}");

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return NotFound(
                    "Interview session not found.");
            }

            if (!response.IsSuccessStatusCode)
            {
                return StatusCode(
                    502,
                    "Unable to retrieve interview details.");
            }

            var session = await response.Content
                .ReadFromJsonAsync<InterviewSessionViewModel>();

            if (session == null)
            {
                return StatusCode(
                    502,
                    "The interview API returned an invalid response.");
            }

            if (session.Status != "InProgress")
            {
                TempData["Error"] =
                    "Only an active interview can be started.";

                return RedirectToAction(
                    "Options", new { id });
            }

            return View(session);
        }


        // GET: /InterviewSessions/Options/41
        [HttpGet]
        public async Task<IActionResult> Options(int id)
        {
            if (id <= 0)
            {
                return BadRequest(
                    "Invalid interview session ID.");
            }

            var client = _httpClientFactory.CreateClient(
                "AuthenticatedInterviewApi");

            var response = await client.GetAsync(
                $"api/InterviewSessions/{id}");

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return NotFound(
                    "Interview session not found.");
            }

            if (!response.IsSuccessStatusCode)
            {
                return StatusCode(
                    502,
                    "Unable to retrieve interview options.");
            }

            var session = await response.Content
                .ReadFromJsonAsync<InterviewSessionViewModel>();

            if (session == null)
            {
                return StatusCode(
                    502,
                    "The interview API returned an invalid response.");
            }

            return View(session);
        }


        // GET: /InterviewSessions/Details/1
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            if (id <= 0)
                return BadRequest();

            var client = _httpClientFactory.CreateClient("AuthenticatedInterviewApi");


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
            {
                return BadRequest();
            }

            var client = _httpClientFactory.CreateClient(
                "AuthenticatedInterviewApi");

            // Retrieve the existing interview session.
            var sessionResponse = await client.GetAsync(
                $"api/InterviewSessions/{sessionId}");

            if (sessionResponse.StatusCode == HttpStatusCode.NotFound)
            {
                return NotFound(
                    "Interview session not found.");
            }

            sessionResponse.EnsureSuccessStatusCode();

            var session = await sessionResponse.Content
                .ReadFromJsonAsync<InterviewSessionViewModel>();

            if (session == null)
            {
                return StatusCode(
                    502,
                    "Invalid session response.");
            }

            if (session.Status != "InProgress")
            {
                return BadRequest(
                    "This interview session is not active.");
            }

            // Retrieve the question assigned to this particular session.
            // The snapshot remains available even if an administrator
            // later edits or deactivates the original question.
            var questionsResponse = await client.GetAsync(
                $"api/InterviewSessions/{sessionId}/questions");

            if (questionsResponse.StatusCode == HttpStatusCode.NotFound)
                return NotFound("Interview session not found.");

            questionsResponse.EnsureSuccessStatusCode();

            var assignedQuestions = await questionsResponse.Content
                .ReadFromJsonAsync<List<InterviewAnswerQuestionDto>>()
                ?? new List<InterviewAnswerQuestionDto>();

            var question = assignedQuestions
                .FirstOrDefault(q => q.Id == questionId);

            if (question == null)
                return NotFound("Question is not assigned to this interview session.");

            // Ensure the question belongs to this category.
            if (question.InterviewCategoryId !=
                session.InterviewCategoryId)
            {
                return BadRequest(
                    "Question does not belong to this interview category.");
            }

            // Prepare the answer form and saved timer information.
            var model = new SubmitInterviewAnswerViewModel
            {
                InterviewSessionId = sessionId,

                InterviewQuestionId = questionId,

                QuestionText = question.QuestionText,

                ActiveDurationSeconds =
                    session.ActiveDurationSeconds,

                IsTimerRunning =
                    session.IsTimerRunning,

                TimerStartedAtUtc =
                    session.TimerStartedAtUtc,

                LastTimerHeartbeatUtc =
                    session.LastTimerHeartbeatUtc
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

            var client = _httpClientFactory.CreateClient("AuthenticatedInterviewApi");

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
                TempData["Success"] =
                    "Your answer was submitted successfully.";

                return RedirectToAction(
                    "Questions",
                    new
                    {
                        id = model.InterviewSessionId,
                        answeredQuestionId = model.InterviewQuestionId
                    });
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
            var client = _httpClientFactory.CreateClient("AuthenticatedInterviewApi");

            var response = await client.GetAsync(
                $"api/InterviewSessions/{model.InterviewSessionId}/questions");

            if (response.IsSuccessStatusCode)
            {
                var questions = await response.Content
                    .ReadFromJsonAsync<List<InterviewAnswerQuestionDto>>();

                model.QuestionText = questions?
                    .FirstOrDefault(q => q.Id == model.InterviewQuestionId)?
                    .QuestionText ?? string.Empty;
            }

            return View("Answer", model);
        }

        private sealed class InterviewAnswerQuestionDto
        {
            public int Id { get; set; }
            public int InterviewCategoryId { get; set; }
            public string QuestionText { get; set; } = string.Empty;
        }



        // GET: /InterviewSessions/Questions/41
        [HttpGet]
        public async Task<IActionResult> Questions(int id)
        {
            if (id <= 0)
            {
                return BadRequest(
                    "Invalid interview session ID.");
            }

            var client = _httpClientFactory.CreateClient(
                "AuthenticatedInterviewApi");

            // Retrieve the existing interview session.
            var sessionResponse = await client.GetAsync(
                $"api/InterviewSessions/{id}");

            if (sessionResponse.StatusCode ==
                HttpStatusCode.NotFound)
            {
                return NotFound(
                    "Interview session not found.");
            }

            sessionResponse.EnsureSuccessStatusCode();

            var session = await sessionResponse.Content
                .ReadFromJsonAsync<InterviewSessionViewModel>();

            if (session == null)
            {
                return StatusCode(
                    502,
                    "Invalid session response.");
            }

            // Retrieve this session's frozen question set rather than
            // the live category question bank.
            var questionsResponse = await client.GetAsync(
                $"api/InterviewSessions/{id}/questions");

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
                question.IsAnswered =
                    answeredQuestionIds.Contains(question.Id);
            }

            // Prepare questions, progress and saved timer data.
            var model = new InterviewSessionQuestionsViewModel
            {
                SessionId = session.Id,

                InterviewCategoryId =
                    session.InterviewCategoryId,

                CategoryName = session.CategoryName,

                Status = session.Status,

                Questions = questions,

                ActiveDurationSeconds =
                    session.ActiveDurationSeconds,

                IsTimerRunning =
                    session.IsTimerRunning,

                TimerStartedAtUtc =
                    session.TimerStartedAtUtc,

                LastTimerHeartbeatUtc =
                    session.LastTimerHeartbeatUtc
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

            var client = _httpClientFactory.CreateClient("AuthenticatedInterviewApi");

            var response = await client.PostAsync(
                $"api/InterviewSessions/{id}/complete",
                null);


            if (response.IsSuccessStatusCode)
            {
                TempData["Success"] =
                    "Your interview has been completed successfully.";

                return RedirectToAction("Results", new { id });
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


        // POST: /InterviewSessions/Reopen/38
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reopen(int id)
        {
            if (id <= 0)
                return BadRequest("Invalid interview session ID.");

            var client = _httpClientFactory.CreateClient(
                "AuthenticatedInterviewApi");

            var response = await client.PostAsync(
                $"api/InterviewSessions/{id}/reopen",
                null);

            if (response.IsSuccessStatusCode)
            {
                TempData["Success"] =
                    "Your interview has been reopened. Continue answering the remaining questions.";

                return RedirectToAction("Questions", new { id });
            }

            if (response.StatusCode == HttpStatusCode.NotFound)
                return NotFound("Interview session not found.");

            if (response.StatusCode == HttpStatusCode.Conflict)
            {
                TempData["Error"] =
                    "This interview cannot be reopened. It may already be active or have no unanswered questions.";
            }
            else
            {
                TempData["Error"] =
                    "Unable to reopen the interview. Please try again.";
            }

            return RedirectToAction("Details", new { id });
        }


        // GET: /InterviewSessions/Results/3
        [HttpGet]
        public async Task<IActionResult> Results(int id)
        {
            if (id <= 0)
                return BadRequest("Invalid interview session ID.");

            var client = _httpClientFactory.CreateClient(
                "AuthenticatedInterviewApi");

            var response = await client.GetAsync(
                $"api/InterviewSessions/{id}/results");

            if (response.StatusCode == HttpStatusCode.NotFound)
                return NotFound("Interview session not found.");

            if (response.StatusCode == HttpStatusCode.BadRequest)
            {
                TempData["Error"] =
                    "Complete the interview before viewing results.";

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

            // The Results API now provides all category questions,
            // including suggested answers for skipped questions.
            var responsesByQuestionId = model.Responses
                .GroupBy(r => r.InterviewQuestionId)
                .ToDictionary(g => g.Key, g => g.First());

            model.Questions = model.Questions
                .Select((question, index) =>
                {
                    responsesByQuestionId.TryGetValue(
                        question.Id,
                        out var answer);

                    question.QuestionNumber = index + 1;
                    question.InterviewQuestionId = question.Id;
                    question.IsAnswered = answer != null;
                    question.AnswerText =
                        answer?.AnswerText ?? string.Empty;
                    question.SubmittedAt = answer?.SubmittedAt;

                    // Show feedback for answered questions only.
                    question.AssessmentFeedback = answer?.AssessmentFeedback;

                    return question;
                })
                .ToList();

            model.TotalQuestions = model.Questions.Count;
            model.TotalAnswered = model.Questions
                .Count(q => q.IsAnswered);

            return View(model);
        }


        // GET: /InterviewSessions/History
        [HttpGet]
        public async Task<IActionResult> History()
        {
            var client = _httpClientFactory.CreateClient("AuthenticatedInterviewApi");


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
            var client = _httpClientFactory.CreateClient("AuthenticatedInterviewApi");


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


        // POST: /InterviewSessions/Begin/41
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Begin(int id)
        {
            if (id <= 0)
            {
                return BadRequest(
                    "Invalid interview session ID.");
            }

            var client = _httpClientFactory.CreateClient(
                "AuthenticatedInterviewApi");

            var response = await client.PostAsync(
                $"api/InterviewSessions/{id}/timer/start",
                null);

            if (response.IsSuccessStatusCode)
            {
                return RedirectToAction(
                    "Questions",
                    new
                    {
                        id,
                        question = 1
                    });
            }

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return NotFound(
                    "Interview session not found.");
            }

            if (response.StatusCode == HttpStatusCode.Conflict)
            {
                TempData["Error"] =
                    "The interview could not be started because " +
                    "its status changed or another request updated it. " +
                    "Please try again.";
            }
            else
            {
                TempData["Error"] =
                    "Unable to start the interview timer. " +
                    "Please try again.";
            }

            return RedirectToAction(
                "Ready",
                new { id });


        }


        // POST: /InterviewSessions/Exit/41
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Exit(int id)
        {
            if (id <= 0)
            {
                return BadRequest();
            }

            var client = _httpClientFactory.CreateClient(
                "AuthenticatedInterviewApi");

            // Pause the timer and save elapsed duration.
            var pauseResponse = await client.PostAsync(
                $"api/InterviewSessions/{id}/timer/pause",
                content: null);

            if (pauseResponse.StatusCode == HttpStatusCode.NotFound)
            {
                return NotFound("Interview session not found.");
            }

            if (pauseResponse.StatusCode == HttpStatusCode.Conflict)
            {
                return Conflict(
                    "The interview session could not be paused. Please try again.");
            }

            if (!pauseResponse.IsSuccessStatusCode)
            {
                return StatusCode(
                    (int)pauseResponse.StatusCode,
                    "Unable to pause the interview timer.");
            }

            // Return to My Interviews without completing
            // or cancelling the session.
            return RedirectToAction("History");
        }



        // POST: /InterviewSessions/Heartbeat/41
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Heartbeat(int id)
        {
            if (id <= 0)
            {
                return BadRequest(
                    "Invalid interview session ID.");
            }

            var client = _httpClientFactory.CreateClient(
                "AuthenticatedInterviewApi");

            var response = await client.PostAsync(
                $"api/InterviewSessions/{id}/timer/heartbeat",
                null);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return NotFound(
                    "Interview session not found.");
            }

            if (response.StatusCode == HttpStatusCode.Conflict)
            {
                return Conflict(
                    "The interview timer was updated by another request.");
            }

            if (!response.IsSuccessStatusCode)
            {
                return StatusCode(
                    (int)response.StatusCode,
                    "Unable to update the interview timer.");
            }

            var timer = await response.Content
                .ReadFromJsonAsync<InterviewTimerHeartbeatViewModel>();

            if (timer == null)
            {
                return StatusCode(
                    502,
                    "Invalid timer response.");
            }

            return Json(new
            {
                timer.Id,
                timer.Status,
                timer.IsTimerRunning,
                timer.ActiveDurationSeconds,
                timer.LastTimerHeartbeatUtc
            });
        }

    }

}
