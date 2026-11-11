
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
    public class InterviewResponsesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public InterviewResponsesController(
            ApplicationDbContext context)
        {
            _context = context;
        }

        private string? GetCurrentUserId()
        {
            return User.FindFirstValue(
                ClaimTypes.NameIdentifier);
        }

        // POST: api/InterviewResponses/submit
        [HttpPost("submit")]
        public async Task<IActionResult> SubmitResponse(
            [FromBody] SubmitInterviewResponseRequest request)
        {
            var userId = GetCurrentUserId();

            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized();

            // Find the session only if it belongs
            // to the authenticated user.
            var session = await _context.InterviewSessions
                .FirstOrDefaultAsync(s =>
                    s.Id == request.InterviewSessionId &&
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
                return BadRequest(new
                {
                    message = "This interview session is not active."
                });
            }

            // Validate the question and its category.
            var question = await _context.InterviewQuestions
                .AsNoTracking()
                .FirstOrDefaultAsync(q =>
                    q.Id == request.InterviewQuestionId &&
                    q.InterviewCategoryId ==
                        session.InterviewCategoryId &&
                    q.IsActive);

            if (question == null)
            {
                return BadRequest(new
                {
                    message = "Invalid question for this interview session."
                });
            }

            // Prevent duplicate answers.
            var alreadyAnswered = await _context.InterviewResponses
                .AnyAsync(r =>
                    r.InterviewSessionId == session.Id &&
                    r.InterviewQuestionId == question.Id);

            if (alreadyAnswered)
            {
                return Conflict(new
                {
                    message = "This question has already been answered."
                });
            }

            // Validate the submitted answer.
            if (string.IsNullOrWhiteSpace(request.AnswerText))
            {
                return BadRequest(new
                {
                    message = "Answer cannot be empty."
                });
            }

            var response = new InterviewResponse
            {
                InterviewSessionId = session.Id,
                InterviewQuestionId = question.Id,
                AnswerText = request.AnswerText.Trim(),
                SubmittedAt = DateTime.UtcNow
            };

            _context.InterviewResponses.Add(response);
            await _context.SaveChangesAsync();

            return Created(
                $"/api/InterviewResponses/{response.Id}",
                new
                {
                    response.Id,
                    response.InterviewSessionId,
                    response.InterviewQuestionId,
                    response.AnswerText,
                    response.SubmittedAt
                });
        }

        // GET: api/InterviewResponses/1
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetResponse(int id)
        {
            var userId = GetCurrentUserId();

            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized();

            var response = await _context.InterviewResponses
                .AsNoTracking()
                .Where(r =>
                    r.Id == id &&
                    r.InterviewSession!.UserId == userId)
                .Select(r => new
                {
                    r.Id,
                    r.InterviewSessionId,
                    r.InterviewQuestionId,
                    r.AnswerText,
                    r.SubmittedAt
                })
                .FirstOrDefaultAsync();

            if (response == null)
            {
                return NotFound();
            }

            return Ok(response);
        }

        // GET: api/InterviewResponses/session/3
        [HttpGet("session/{sessionId:int}")]
        public async Task<IActionResult> GetSessionResponses(
            int sessionId)
        {
            var userId = GetCurrentUserId();

            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized();

            if (sessionId <= 0)
            {
                return BadRequest(
                    "Invalid interview session ID.");
            }

            // Check ownership before retrieving responses.
            var sessionExists = await _context.InterviewSessions
                .AnyAsync(s =>
                    s.Id == sessionId &&
                    s.UserId == userId);

            if (!sessionExists)
            {
                return NotFound(
                    "Interview session not found.");
            }

            var responses = await _context.InterviewResponses
                .AsNoTracking()
                .Where(r =>
                    r.InterviewSessionId == sessionId &&
                    r.InterviewSession!.UserId == userId)
                .OrderBy(r => r.InterviewQuestionId)
                .Select(r => new
                {
                    r.Id,
                    r.InterviewSessionId,
                    r.InterviewQuestionId,
                    r.SubmittedAt
                })
                .ToListAsync();

            return Ok(responses);
        }
    }
}
