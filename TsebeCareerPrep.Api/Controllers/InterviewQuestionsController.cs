
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TsebeCareerPrep.Api.Data;
using Microsoft.AspNetCore.Authorization;
using TsebeCareerPrep.Api.Models;

namespace TsebeCareerPrep.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class InterviewQuestionsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public InterviewQuestionsController(
            ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/InterviewQuestions
        [HttpGet]
        public async Task<IActionResult> GetQuestions()
        {
            var questions = await _context.InterviewQuestions
                .AsNoTracking()
                .Where(q => q.IsActive)
                .OrderBy(q => q.Id)
                .Select(q => new
                {
                    q.Id,
                    q.InterviewCategoryId,
                    q.QuestionText,
                    q.DifficultyLevel
                })
                .ToListAsync();

            return Ok(questions);
        }

        // GET: api/InterviewQuestions/5
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetQuestion(int id)
        {
            var question = await _context.InterviewQuestions
                .AsNoTracking()
                .Where(q => q.Id == id && q.IsActive)
                .Select(q => new
                {
                    q.Id,
                    q.InterviewCategoryId,
                    q.QuestionText,
                    q.DifficultyLevel
                })
                .FirstOrDefaultAsync();

            if (question == null)
                return NotFound();

            return Ok(question);
        }

        // GET: api/InterviewQuestions/category/1
        [HttpGet("category/{categoryId:int}")]
        public async Task<IActionResult> GetQuestionsByCategory(
            int categoryId)
        {
            var categoryExists = await _context.InterviewCategories
                .AnyAsync(c => c.Id == categoryId && c.IsActive);

            if (!categoryExists)
                return NotFound("Interview category not found.");

            var questions = await _context.InterviewQuestions
                .AsNoTracking()
                .Where(q =>
                    q.InterviewCategoryId == categoryId &&
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

            return Ok(questions);
        }

        // GET: api/InterviewQuestions/admin/all
        // Administrator-only question management listing.
        [HttpGet("admin/all")]
        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> GetAllQuestionsForAdmin()
        {
            var questions = await _context.InterviewQuestions
                .AsNoTracking()
                .OrderBy(q => q.InterviewCategoryId)
                .ThenBy(q => q.Id)
                .Select(q => new
                {
                    q.Id,
                    q.InterviewCategoryId,
                    q.QuestionText,
                    q.SuggestedAnswer,
                    q.DifficultyLevel,
                    q.IsActive
                })
                .ToListAsync();

            return Ok(questions);
        }


        // POST: api/InterviewQuestions/admin
        // Create a new interview question.
        [HttpPost("admin")]
        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> CreateQuestion(
            [FromBody] CreateInterviewQuestionRequest request)
        {
            // Verify that the selected category exists and is active.
            var categoryExists = await _context.InterviewCategories
                .AnyAsync(c =>
                    c.Id == request.InterviewCategoryId &&
                    c.IsActive);

            if (!categoryExists)
            {
                return BadRequest(new
                {
                    message = "The selected interview category is invalid or inactive."
                });
            }

            // Validate supported difficulty levels.
            var validDifficultyLevels = new[]
            {
                "Beginner",
                "Intermediate",
                "Advanced"
            };

            if (!validDifficultyLevels.Contains(
                request.DifficultyLevel,
                StringComparer.OrdinalIgnoreCase))
            {
                return BadRequest(new
                {
                    message = "Difficulty level must be Beginner, Intermediate or Advanced."
                });
            }

            var question = new InterviewQuestion
            {
                InterviewCategoryId = request.InterviewCategoryId,
                QuestionText = request.QuestionText.Trim(),
                SuggestedAnswer = request.SuggestedAnswer?.Trim(),
                DifficultyLevel = validDifficultyLevels.First(
                    level => level.Equals(
                        request.DifficultyLevel,
                        StringComparison.OrdinalIgnoreCase)),
                IsActive = request.IsActive
            };

            _context.InterviewQuestions.Add(question);

            await _context.SaveChangesAsync();

            return StatusCode(
                StatusCodes.Status201Created,
                new
                {
                    message = "Interview question created successfully.",
                    question.Id,
                    question.InterviewCategoryId,
                    question.QuestionText,
                    question.SuggestedAnswer,
                    question.DifficultyLevel,
                    question.IsActive
                });
        }


        // PUT: api/InterviewQuestions/admin/9
        // Administrator-only interview question update.
        [HttpPut("admin/{id:int}")]
        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> UpdateQuestion(
            int id,
            [FromBody] UpdateInterviewQuestionRequest request)
        {
            var question = await _context.InterviewQuestions
                .FirstOrDefaultAsync(q => q.Id == id);

            if (question == null)
            {
                return NotFound(new
                {
                    message = "Interview question not found."
                });
            }

            var categoryExists = await _context.InterviewCategories
                .AnyAsync(c =>
                    c.Id == request.InterviewCategoryId &&
                    c.IsActive);

            if (!categoryExists)
            {
                return BadRequest(new
                {
                    message = "The selected interview category is invalid or inactive."
                });
            }

            var validDifficultyLevels = new[]
            {
                "Beginner",
                "Intermediate",
                "Advanced"
            };

            var difficulty = validDifficultyLevels
                .FirstOrDefault(level => level.Equals(
                    request.DifficultyLevel,
                    StringComparison.OrdinalIgnoreCase));

            if (difficulty == null)
            {
                return BadRequest(new
                {
                    message = "Difficulty level must be Beginner, Intermediate or Advanced."
                });
            }

            question.InterviewCategoryId = request.InterviewCategoryId;
            question.QuestionText = request.QuestionText.Trim();
            question.SuggestedAnswer = request.SuggestedAnswer?.Trim();
            question.DifficultyLevel = difficulty;
            question.IsActive = request.IsActive;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Interview question updated successfully.",
                question.Id,
                question.InterviewCategoryId,
                question.QuestionText,
                question.SuggestedAnswer,
                question.DifficultyLevel,
                question.IsActive
            });
        }



    }
}
