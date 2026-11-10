
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TsebeCareerPrep.Api.Data;
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
        public async Task<ActionResult<IEnumerable<InterviewQuestion>>>
            GetQuestions()
        {
            var questions = await _context.InterviewQuestions
                .AsNoTracking()
                .Where(q => q.IsActive)
                .OrderBy(q => q.Id)
                .ToListAsync();

            return Ok(questions);
        }

        // GET: api/InterviewQuestions/5
        [HttpGet("{id:int}")]
        public async Task<ActionResult<InterviewQuestion>>
            GetQuestion(int id)
        {
            var question = await _context.InterviewQuestions
                .AsNoTracking()
                .FirstOrDefaultAsync(q =>
                    q.Id == id && q.IsActive);

            if (question == null)
                return NotFound();

            return Ok(question);
        }

        // GET: api/InterviewQuestions/category/1
        [HttpGet("category/{categoryId:int}")]
        public async Task<ActionResult<IEnumerable<InterviewQuestion>>>
            GetQuestionsByCategory(int categoryId)
        {
            var categoryExists = await _context.InterviewCategories
                .AnyAsync(c => c.Id == categoryId && c.IsActive);

            if (!categoryExists)
                return NotFound("Interview category not found.");

            var questions = await _context.InterviewQuestions
                .AsNoTracking()
                .Where(q => q.InterviewCategoryId == categoryId
                         && q.IsActive)
                .OrderBy(q => q.Id)
                .ToListAsync();

            return Ok(questions);
        }
    }
}
