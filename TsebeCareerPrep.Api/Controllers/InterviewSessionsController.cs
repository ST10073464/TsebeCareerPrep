
using System.Security.Claims;
using Microsoft.Data.SqlClient;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TsebeCareerPrep.Api.Data;
using TsebeCareerPrep.Api.Models;
using TsebeCareerPrep.Api.Services;

namespace TsebeCareerPrep.Api.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class InterviewSessionsController : ControllerBase
    {

        private readonly ApplicationDbContext _context;
        private readonly IInterviewAssessmentService _assessmentService;

        public InterviewSessionsController(
            ApplicationDbContext context,
            IInterviewAssessmentService assessmentService)
        {
            _context = context;
            _assessmentService = assessmentService;
        }


        private string? GetCurrentUserId()
        {
            return User.FindFirstValue(
                ClaimTypes.NameIdentifier);
        }

        // POST: api/InterviewSessions/start
        [HttpPost("start")]
        public async Task<IActionResult> StartInterview(
            [FromBody] StartInterviewRequest request)
        {
            var userId = GetCurrentUserId();

            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized();

            var category = await _context.InterviewCategories
                .AsNoTracking()
                .FirstOrDefaultAsync(c =>
                    c.Id == request.InterviewCategoryId &&
                    c.IsActive);

            if (category == null)
            {
                return BadRequest(new
                {
                    message = "Invalid or inactive interview category."
                });
            }

            // Reuse an existing interview instead of violating the
            // unique active-session index for this user and category.
            var existingSession = await _context.InterviewSessions
                .AsNoTracking()
                .FirstOrDefaultAsync(s =>
                    s.UserId == userId &&
                    s.InterviewCategoryId == category.Id &&
                    s.Status == "InProgress");

            if (existingSession != null)
            {
                return Ok(new
                {
                    existingSession.Id,
                    existingSession.InterviewCategoryId,
                    CategoryName = category.Name,
                    existingSession.StartedAt,
                    existingSession.Status,
                    IsExistingSession = true,
                    message = "Continue your existing interview."
                });
            }

            // Freeze the active question set for new sessions only.
            var questions = await _context.InterviewQuestions
                .AsNoTracking()
                .Where(q =>
                    q.InterviewCategoryId == category.Id &&
                    q.IsActive)
                .OrderBy(q => q.Id)
                .ToListAsync();

            if (questions.Count == 0)
            {
                return BadRequest(new
                {
                    message = "This interview category has no active questions."
                });
            }

            var assignedAtUtc = DateTime.UtcNow;
            var session = new InterviewSession
            {
                InterviewCategoryId = category.Id,
                UserId = userId,
                StartedAt = assignedAtUtc,
                Status = "InProgress"
            };

            // Save the session and snapshots atomically.
            await using var transaction =
                await _context.Database.BeginTransactionAsync();

            try
            {
                _context.InterviewSessions.Add(session);
                await _context.SaveChangesAsync();

                var snapshots = questions
                    .Select((question, index) =>
                        new InterviewSessionQuestion
                        {
                            InterviewSessionId = session.Id,
                            InterviewQuestionId = question.Id,
                            QuestionText = question.QuestionText,
                            SuggestedAnswer = question.SuggestedAnswer,
                            DifficultyLevel = question.DifficultyLevel,
                            DisplayOrder = index + 1,
                            AssignedAtUtc = assignedAtUtc
                        })
                    .ToList();

                _context.InterviewSessionQuestions.AddRange(snapshots);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch (DbUpdateException ex) when (
                ex.InnerException is SqlException sqlException &&
                (sqlException.Number == 2601 || sqlException.Number == 2627))
            {
                // Another request may have started the same interview
                // after the initial existence check. Roll back before
                // looking up the session created by that request.
                await transaction.RollbackAsync();
                _context.ChangeTracker.Clear();

                var concurrentSession = await _context.InterviewSessions
                    .AsNoTracking()
                    .FirstOrDefaultAsync(s =>
                        s.UserId == userId &&
                        s.InterviewCategoryId == category.Id &&
                        s.Status == "InProgress");

                if (concurrentSession == null)
                    throw;

                return Ok(new
                {
                    concurrentSession.Id,
                    concurrentSession.InterviewCategoryId,
                    CategoryName = category.Name,
                    concurrentSession.StartedAt,
                    concurrentSession.Status,
                    IsExistingSession = true,
                    message = "Continue your existing interview."
                });
            }

            return CreatedAtAction(
                nameof(GetSession),
                new { id = session.Id },
                new
                {
                    session.Id,
                    session.InterviewCategoryId,
                    CategoryName = category.Name,
                    session.StartedAt,
                    session.Status,
                    IsExistingSession = false
                });
        }


        // POST: api/InterviewSessions/41/timer/start
        [HttpPost("{id:int}/timer/start")]
        public async Task<IActionResult> StartTimer(int id)
        {
            var userId = GetCurrentUserId();

            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized();

            var session = await _context.InterviewSessions
                .FirstOrDefaultAsync(s =>
                    s.Id == id &&
                    s.UserId == userId);

            if (session == null)
            {
                return NotFound(new
                {
                    message = "Interview session not found."
                });
            }

            if (session.Status != "InProgress")
            {
                return Conflict(new
                {
                    message =
                        "The timer can only run for an interview in progress."
                });
            }

            InterviewTimerService.Start(
                session,
                DateTime.UtcNow);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                return Conflict(new
                {
                    message =
                        "The interview session was updated by another request. " +
                        "Please refresh and try again."
                });
            }

            return Ok(new
            {
                session.Id,
                session.Status,
                session.IsTimerRunning,
                session.ActiveDurationSeconds,
                session.TimerStartedAtUtc
            });
        }


        // GET: api/InterviewSessions/53/questions
        // Return the questions assigned to this user's interview.
        // Do not expose suggested answers during an interview.
        [HttpGet("{id:int}/questions")]
        public async Task<IActionResult> GetSessionQuestions(int id)
        {
            var userId = GetCurrentUserId();

            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized();

            if (id <= 0)
                return BadRequest(new
                {
                    message = "Invalid interview session ID."
                });

            var session = await _context.InterviewSessions
                .AsNoTracking()
                .Where(s => s.Id == id && s.UserId == userId)
                .Select(s => new
                {
                    s.Id,
                    s.InterviewCategoryId
                })
                .FirstOrDefaultAsync();

            if (session == null)
                return NotFound(new
                {
                    message = "Interview session not found."
                });

            // Snapshots freeze the question set, including questions that
            // an administrator later edits or deactivates.
            var assignedQuestions = await _context.InterviewSessionQuestions
                .AsNoTracking()
                .Where(sq => sq.InterviewSessionId == session.Id)
                .OrderBy(sq => sq.DisplayOrder)
                .Select(sq => new
                {
                    Id = sq.InterviewQuestionId,
                    InterviewCategoryId = session.InterviewCategoryId,
                    sq.QuestionText,
                    sq.DifficultyLevel,
                    sq.DisplayOrder
                })
                .ToListAsync();

            if (assignedQuestions.Count > 0)
                return Ok(assignedQuestions);

            // Backward compatibility: sessions created before snapshots
            // existed still use the active questions in their category.
            var legacyQuestions = await _context.InterviewQuestions
                .AsNoTracking()
                .Where(q =>
                    q.InterviewCategoryId == session.InterviewCategoryId &&
                    q.IsActive)
                .OrderBy(q => q.Id)
                .Select(q => new
                {
                    q.Id,
                    q.InterviewCategoryId,
                    q.QuestionText,
                    q.DifficultyLevel
                })
                .ToListAsync();

            return Ok(legacyQuestions);
        }


        // GET: api/InterviewSessions/1
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetSession(int id)
        {
            var userId = GetCurrentUserId();

            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized();

            var session = await _context.InterviewSessions
                .AsNoTracking()
                .Where(s =>
                    s.Id == id &&
                    s.UserId == userId)

                .Select(s => new
                {
                    s.Id,
                    s.InterviewCategoryId,
                    CategoryName = s.InterviewCategory!.Name,
                    s.StartedAt,
                    s.CompletedAt,
                    s.Status,

                    // Interview timer information
                    s.ActiveDurationSeconds,
                    s.IsTimerRunning,
                    s.TimerStartedAtUtc,
                    s.LastTimerHeartbeatUtc
                })

                .FirstOrDefaultAsync();

            if (session == null)
            {
                return NotFound(new
                {
                    message = "Interview session not found."
                });
            }

            return Ok(session);
        }

        // POST: api/InterviewSessions/41/restart
        [HttpPost("{id:int}/restart")]
        public async Task<IActionResult> RestartInterview(int id)
        {
            var userId = GetCurrentUserId();

            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized();

            if (id <= 0)
                return BadRequest(new
                {
                    message = "Invalid interview session ID."
                });

            await using var transaction =
                await _context.Database.BeginTransactionAsync();

            // Only the owner may restart an active session.
            var session = await _context.InterviewSessions
                .FirstOrDefaultAsync(s =>
                    s.Id == id &&
                    s.UserId == userId);

            if (session == null)
                return NotFound(new
                {
                    message = "Interview session not found."
                });

            if (session.Status != "InProgress")
                return Conflict(new
                {
                    message =
                        "Only an interview in progress can be restarted."
                });

            // Load responses for this session only.
            // Related assessments are deleted by the
            // database's verified cascade relationship.
            var responses = await _context.InterviewResponses
                .Where(r => r.InterviewSessionId == session.Id)
                .ToListAsync();

            _context.InterviewResponses.RemoveRange(responses);


            // Preserve the existing session ID and category.
            session.StartedAt = DateTime.UtcNow;
            session.CompletedAt = null;
            session.Status = "InProgress";

            // Reset accumulated interview time.
            // The timer remains paused until Begin Interview is clicked.
            InterviewTimerService.Reset(session);


            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return Ok(new
            {
                session.Id,
                session.InterviewCategoryId,
                session.StartedAt,
                session.Status,
                TotalAnswered = 0,
                message =
                    "Interview restarted successfully. Ready to begin."
            });
        }



        // POST: api/InterviewSessions/41/timer/pause
        [HttpPost("{id:int}/timer/pause")]
        public async Task<IActionResult> PauseTimer(int id)
        {
            var userId = GetCurrentUserId();

            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized();

            var session = await _context.InterviewSessions
                .FirstOrDefaultAsync(s =>
                    s.Id == id &&
                    s.UserId == userId);

            if (session == null)
            {
                return NotFound(new
                {
                    message = "Interview session not found."
                });
            }

            InterviewTimerService.Pause(
                session,
                DateTime.UtcNow);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                return Conflict(new
                {
                    message =
                        "The interview session was updated by another request. " +
                        "Please refresh and try again."
                });
            }

            return Ok(new
            {
                session.Id,
                session.Status,
                session.IsTimerRunning,
                session.ActiveDurationSeconds,
                message = "Interview timer paused."
            });
        }



        // POST: api/InterviewSessions/41/timer/heartbeat
        [HttpPost("{id:int}/timer/heartbeat")]
        public async Task<IActionResult> TimerHeartbeat(int id)
        {
            var userId = GetCurrentUserId();

            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized();

            var session = await _context.InterviewSessions
                .FirstOrDefaultAsync(s =>
                    s.Id == id &&
                    s.UserId == userId);

            if (session == null)
            {
                return NotFound(new
                {
                    message = "Interview session not found."
                });
            }

            if (session.Status != "InProgress")
            {
                return Conflict(new
                {
                    message =
                        "The timer is only available for an interview in progress."
                });
            }

            if (!session.IsTimerRunning)
            {
                return Conflict(new
                {
                    message = "The interview timer is paused."
                });
            }

            InterviewTimerService.Heartbeat(
                session,
                DateTime.UtcNow);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                return Conflict(new
                {
                    message =
                        "The interview session was updated by another request. " +
                        "Please refresh and try again."
                });
            }

            return Ok(new
            {
                session.Id,
                session.Status,
                session.IsTimerRunning,
                session.ActiveDurationSeconds,
                session.LastTimerHeartbeatUtc
            });
        }



        // POST: api/InterviewSessions/1/complete
        [HttpPost("{id:int}/complete")]
        public async Task<IActionResult> CompleteInterview(int id)
        {
            var userId = GetCurrentUserId();

            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized();

            var session = await _context.InterviewSessions
                .FirstOrDefaultAsync(s =>
                    s.Id == id &&
                    s.UserId == userId);

            if (session == null)
            {
                return NotFound(new
                {
                    message = "Interview session not found."
                });
            }

            if (session.Status == "Completed")
            {
                return Conflict(new
                {
                    message = "This interview session is already completed."
                });
            }

            if (session.Status != "InProgress")
            {
                return BadRequest(new
                {
                    message = "Only an active interview session can be completed."
                });
            }

            var hasResponses = await _context.InterviewResponses
                .AnyAsync(r =>
                    r.InterviewSessionId == id &&
                    r.InterviewSession!.UserId == userId);

            if (!hasResponses)
            {
                return BadRequest(new
                {
                    message = "Answer at least one question before completing the interview."
                });
            }


            var completedAtUtc = DateTime.UtcNow;

            // Stop the timer and preserve the accumulated duration.
            InterviewTimerService.Pause(session, completedAtUtc);

            // Mark the interview as completed.
            session.Status = "Completed";
            session.CompletedAt = completedAtUtc;


            await _context.SaveChangesAsync();

            return Ok(new
            {
                session.Id,
                session.Status,
                session.StartedAt,
                session.CompletedAt,
                message = "Interview session completed successfully."
            });
        }

        // POST: api/InterviewSessions/1/reopen
        [HttpPost("{id:int}/reopen")]
        public async Task<IActionResult> ReopenInterview(int id)
        {
            var userId = GetCurrentUserId();

            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized();

            var session = await _context.InterviewSessions
                .FirstOrDefaultAsync(s =>
                    s.Id == id &&
                    s.UserId == userId);

            if (session == null)
            {
                return NotFound(new
                {
                    message = "Interview session not found."
                });
            }

            if (session.Status != "Completed")
            {
                return Conflict(new
                {
                    message = "Only completed interviews can be reopened."
                });
            }

            // Use the frozen question set for sessions with snapshots.
            // Legacy sessions continue to use their original active-question logic.
            var hasSnapshots = await _context.InterviewSessionQuestions
                .AnyAsync(sq => sq.InterviewSessionId == id);

            int totalQuestions;
            int answeredQuestions;

            if (hasSnapshots)
            {
                totalQuestions = await _context.InterviewSessionQuestions
                    .CountAsync(sq => sq.InterviewSessionId == id);

                answeredQuestions = await _context.InterviewResponses
                    .Where(r => r.InterviewSessionId == id &&
                        _context.InterviewSessionQuestions.Any(sq =>
                            sq.InterviewSessionId == id &&
                            sq.InterviewQuestionId == r.InterviewQuestionId))
                    .Select(r => r.InterviewQuestionId)
                    .Distinct()
                    .CountAsync();
            }
            else
            {
                totalQuestions = await _context.InterviewQuestions
                    .CountAsync(q =>
                        q.InterviewCategoryId == session.InterviewCategoryId &&
                        q.IsActive);

                answeredQuestions = await _context.InterviewResponses
                    .Where(r =>
                        r.InterviewSessionId == id &&
                        r.InterviewQuestion != null &&
                        r.InterviewQuestion.InterviewCategoryId ==
                            session.InterviewCategoryId &&
                        r.InterviewQuestion.IsActive)
                    .Select(r => r.InterviewQuestionId)
                    .Distinct()
                    .CountAsync();
            }

            if (totalQuestions == 0 ||
                answeredQuestions >= totalQuestions)
            {
                return Conflict(new
                {
                    message = "There are no unanswered questions to complete."
                });
            }

            session.Status = "InProgress";
            session.CompletedAt = null;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                session.Id,
                session.Status,
                session.StartedAt,
                session.CompletedAt,
                TotalQuestions = totalQuestions,
                TotalAnswered = answeredQuestions,
                RemainingQuestions = totalQuestions - answeredQuestions,
                message = "Interview reopened successfully."
            });
        }

        // GET: api/InterviewSessions/1/results
        [HttpGet("{id:int}/results")]
        public async Task<IActionResult> GetSessionResults(int id)
        {
            var userId = GetCurrentUserId();

            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized();

            var session = await _context.InterviewSessions
                .AsNoTracking()
                .Where(s =>
                    s.Id == id &&
                    s.UserId == userId)
                .Select(s => new
                {
                    s.Id,
                    s.InterviewCategoryId,
                    CategoryName = s.InterviewCategory!.Name,
                    s.StartedAt,
                    s.CompletedAt,
                    s.Status,
                    s.ActiveDurationSeconds
                })
                .FirstOrDefaultAsync();

            if (session == null)
            {
                return NotFound(new
                {
                    message = "Interview session not found."
                });
            }

            if (session.Status != "Completed")
            {
                return BadRequest(new
                {
                    message =
                        "Complete the interview before viewing results."
                });
            }

            // Read assigned question snapshots for this session. Older
            // sessions have no snapshots and retain the legacy behaviour.
            var snapshots = await _context.InterviewSessionQuestions
                .AsNoTracking()
                .Where(sq => sq.InterviewSessionId == id)
                .OrderBy(sq => sq.DisplayOrder)
                .Select(sq => new
                {
                    sq.InterviewQuestionId,
                    sq.QuestionText,
                    sq.DifficultyLevel,
                    sq.SuggestedAnswer
                })
                .ToListAsync();

            var snapshotsByQuestionId = snapshots.ToDictionary(
                sq => sq.InterviewQuestionId);

            var responses = await _context.InterviewResponses
                .AsNoTracking()
                .Where(r =>
                    r.InterviewSessionId == id &&
                    r.InterviewSession!.UserId == userId)
                .OrderBy(r => r.SubmittedAt)
                .Select(r => new
                {
                    r.Id,
                    r.InterviewQuestionId,
                    QuestionText = r.InterviewQuestion!.QuestionText,
                    DifficultyLevel = r.InterviewQuestion.DifficultyLevel,
                    r.AnswerText,
                    SuggestedAnswer = r.InterviewQuestion.SuggestedAnswer,
                    r.SubmittedAt
                })
                .ToListAsync();

            // Keep the exact existing question response structure.
            // For snapshot sessions, do not filter out deactivated questions.
            var questions = snapshots.Count > 0
                ? snapshots.Select(sq => new
                {
                    Id = sq.InterviewQuestionId,
                    sq.QuestionText,
                    sq.DifficultyLevel,
                    sq.SuggestedAnswer
                }).ToList()
                : await _context.InterviewQuestions
                    .AsNoTracking()
                    .Where(q =>
                        q.InterviewCategoryId == session.InterviewCategoryId &&
                        q.IsActive)
                    .OrderBy(q => q.Id)
                    .Select(q => new
                    {
                        q.Id,
                        q.QuestionText,
                        q.DifficultyLevel,
                        q.SuggestedAnswer
                    })
                    .ToListAsync();

            // Retrieve feedback only for responses belonging
            // to this authenticated user's completed session.
            var feedbackByResponseId =
                new Dictionary<int, InterviewAssessmentFeedback>();

            foreach (var response in responses)
            {
                var feedback =
                    await _assessmentService.GetFeedbackAsync(
                        response.Id);

                if (feedback != null)
                {
                    feedbackByResponseId[response.Id] =
                        feedback;
                }
            }

            // Preserve existing response properties and add
            // optional assessment feedback.
            var responsesWithFeedback = responses
                .Select(r =>
                {
                    var hasSnapshot = snapshotsByQuestionId.TryGetValue(
                        r.InterviewQuestionId, out var snapshot);

                    return new
                    {
                        r.Id,
                        r.InterviewQuestionId,
                        QuestionText = hasSnapshot
                            ? snapshot!.QuestionText : r.QuestionText,
                        DifficultyLevel = hasSnapshot
                            ? snapshot!.DifficultyLevel : r.DifficultyLevel,
                        r.AnswerText,
                        SuggestedAnswer = hasSnapshot
                            ? snapshot!.SuggestedAnswer : r.SuggestedAnswer,
                        r.SubmittedAt,
                        AssessmentFeedback =
                            feedbackByResponseId.TryGetValue(
                                r.Id, out var feedback)
                                ? feedback
                                : null
                    };
                })
                .ToList();

            return Ok(new
            {
                Session = session,
                TotalAnswered = responses.Count,
                Responses = responsesWithFeedback,
                Questions = questions
            });
        }

        // GET: api/InterviewSessions/history
        [HttpGet("history")]
        public async Task<IActionResult> GetInterviewHistory()
        {
            var userId = GetCurrentUserId();

            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized();

            var sessions = await _context.InterviewSessions
                .AsNoTracking()
                .Where(s => s.UserId == userId)
                .OrderByDescending(s => s.StartedAt)
                .Select(s => new
                {
                    s.Id,
                    s.InterviewCategoryId,
                    CategoryName = s.InterviewCategory!.Name,
                    s.Status,
                    s.StartedAt,
                    s.CompletedAt,
                    s.ActiveDurationSeconds,
                    TotalAnswered = _context.InterviewResponses
                        .Count(r =>
                            r.InterviewSessionId == s.Id)
                })
                .ToListAsync();

            return Ok(sessions);
        }

        // GET: api/InterviewSessions/progress
        [HttpGet("progress")]
        public async Task<IActionResult> GetInterviewProgress()
        {
            var userId = GetCurrentUserId();

            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized();

            var totalInterviews =
                await _context.InterviewSessions
                    .CountAsync(s => s.UserId == userId);

            var completedInterviews =
                await _context.InterviewSessions
                    .CountAsync(s =>
                        s.UserId == userId &&
                        s.Status == "Completed");

            var inProgressInterviews =
                await _context.InterviewSessions
                    .CountAsync(s =>
                        s.UserId == userId &&
                        s.Status == "InProgress");

            var totalAnswers =
                await _context.InterviewResponses
                    .CountAsync(r =>
                        r.InterviewSession!.UserId == userId);

            var completionRate = totalInterviews > 0
                ? Math.Round(
                    (double)completedInterviews
                    / totalInterviews * 100, 1)
                : 0;

            return Ok(new
            {
                TotalInterviews = totalInterviews,
                CompletedInterviews = completedInterviews,
                InProgressInterviews = inProgressInterviews,
                TotalAnswers = totalAnswers,
                CompletionRate = completionRate
            });
        }
    }
}
