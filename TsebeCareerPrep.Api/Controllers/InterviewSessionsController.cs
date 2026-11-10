
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TsebeCareerPrep.Api.Data;
using TsebeCareerPrep.Api.Models;

namespace TsebeCareerPrep.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class InterviewSessionsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public InterviewSessionsController(
            ApplicationDbContext context)
        {
            _context = context;
        }

        // POST: api/InterviewSessions/start
        [HttpPost("start")]
        public async Task<IActionResult> StartInterview(
            [FromBody] StartInterviewRequest request)
        {
            // Verify that the selected category exists.
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

            // Create a new interview session.
            var session = new InterviewSession
            {
                InterviewCategoryId = category.Id,
                StartedAt = DateTime.UtcNow,
                Status = "InProgress"
            };

            // Save the session to SQL Server.
            _context.InterviewSessions.Add(session);
            await _context.SaveChangesAsync();

            // Return the newly created session.
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
            var session = await _context.InterviewSessions
                .AsNoTracking()
                .Where(s => s.Id == id)
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
            var session = await _context.InterviewSessions
                .FirstOrDefaultAsync(s => s.Id == id);

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
                .AnyAsync(r => r.InterviewSessionId == id);

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
            var session = await _context.InterviewSessions
                .AsNoTracking()
                .Where(s => s.Id == id)
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
                .Where(r => r.InterviewSessionId == id)
                .OrderBy(r => r.SubmittedAt)
                .Select(r => new
                {
                    r.Id,
                    r.InterviewQuestionId,
                    QuestionText = r.InterviewQuestion!.QuestionText,
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


    }

}
