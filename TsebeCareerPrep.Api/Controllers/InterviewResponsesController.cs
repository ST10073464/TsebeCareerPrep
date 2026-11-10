
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TsebeCareerPrep.Api.Data;
using TsebeCareerPrep.Api.Models;

namespace TsebeCareerPrep.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class InterviewResponsesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public InterviewResponsesController(
            ApplicationDbContext context)
        {
            _context = context;
        }

        // POST: api/InterviewResponses/submit
        [HttpPost("submit")]
        public async Task<IActionResult> SubmitResponse(
            [FromBody] SubmitInterviewResponseRequest request)
        {
            // Check that the interview session exists.
            var session = await _context.InterviewSessions
                .FirstOrDefaultAsync(s =>
                    s.Id == request.InterviewSessionId);

            if (session == null)
            {
                return NotFound(new
                {
                    message = "Interview session not found."
                });
            }

            // Check that the session is still active.
            if (session.Status != "InProgress")
            {
                return BadRequest(new
                {
                    message = "This interview session is not active."
                });
            }

            // Check that the question exists and belongs
            // to the selected interview category.
            var question = await _context.InterviewQuestions
                .AsNoTracking()
                .FirstOrDefaultAsync(q =>
                    q.Id == request.InterviewQuestionId &&
                    q.InterviewCategoryId == session.InterviewCategoryId &&
                    q.IsActive);

            if (question == null)
            {
                return BadRequest(new
                {
                    message = "Invalid question for this interview session."
                });
            }

            // Prevent duplicate answers for the same question.
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

            // Save the user's answer.
            var response = new InterviewResponse
            {
                InterviewSessionId = session.Id,
                InterviewQuestionId = question.Id,
                AnswerText = request.AnswerText.Trim(),
                SubmittedAt = DateTime.UtcNow
            };

            if (string.IsNullOrWhiteSpace(response.AnswerText))
            {
                return BadRequest(new
                {
                    message = "Answer cannot be empty."
                });
            }

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
            var response = await _context.InterviewResponses
                .AsNoTracking()
                .Where(r => r.Id == id)
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
        public async Task<IActionResult> GetSessionResponses(int sessionId)
        {
            if (sessionId <= 0)
            {
                return BadRequest("Invalid interview session ID.");
            }

            // Confirm the session exists.
            var sessionExists = await _context.InterviewSessions
                .AnyAsync(s => s.Id == sessionId);

            if (!sessionExists)
            {
                return NotFound("Interview session not found.");
            }

            // Retrieve answered question IDs for this session.
            var responses = await _context.InterviewResponses
                .AsNoTracking()
                .Where(r => r.InterviewSessionId == sessionId)
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
