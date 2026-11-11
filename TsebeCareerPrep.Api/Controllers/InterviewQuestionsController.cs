
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TsebeCareerPrep.Api.Data;

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
    }
}
