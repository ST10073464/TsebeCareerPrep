
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TsebeCareerPrep.Api.Data;
using TsebeCareerPrep.Api.Models;

namespace TsebeCareerPrep.Api.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class InterviewSessionsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public InterviewSessionsController(
            ApplicationDbContext context)
        {
            _context = context;
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

            var session = new InterviewSession
            {
                InterviewCategoryId = category.Id,
                UserId = userId,
                StartedAt = DateTime.UtcNow,
                Status = "InProgress"
            };

            _context.InterviewSessions.Add(session);
            await _context.SaveChangesAsync();

            return CreatedAtAction(
                nameof(GetSession),
                new { id = session.Id },
                new
                {
                    session.Id,
                    session.InterviewCategoryId,
                    CategoryName = category.Name,
                    session.StartedAt,
                    session.Status
                });
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
                    s.Status
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

            session.Status = "Completed";
            session.CompletedAt = DateTime.UtcNow;

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
                    s.Status
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
                    message = "Complete the interview before viewing results."
                });
            }

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
                    QuestionText =
                        r.InterviewQuestion!.QuestionText,
                    DifficultyLevel =
                        r.InterviewQuestion.DifficultyLevel,
                    r.AnswerText,
                    SuggestedAnswer =
                        r.InterviewQuestion.SuggestedAnswer,
                    r.SubmittedAt
                })
                .ToListAsync();

            return Ok(new
            {
                Session = session,
                TotalAnswered = responses.Count,
                Responses = responses
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
