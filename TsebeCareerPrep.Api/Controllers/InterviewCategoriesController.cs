
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TsebeCareerPrep.Api.Data;
using TsebeCareerPrep.Api.Models;
using Microsoft.AspNetCore.Authorization;

namespace TsebeCareerPrep.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class InterviewCategoriesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public InterviewCategoriesController(
            ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<InterviewCategory>>> GetCategories()
        {
            var categories = await _context.InterviewCategories
                .AsNoTracking()
                .Where(c => c.IsActive)
                .OrderBy(c => c.Id)
                .ToListAsync();

            return Ok(categories);
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<InterviewCategory>> GetCategory(int id)
        {
            var category = await _context.InterviewCategories
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == id && c.IsActive);

            if (category == null)
            {
                return NotFound();
            }

            return Ok(category);
        }


        // =====================================================
        // GET: api/InterviewCategories/admin/all
        // Administrator: View active and inactive categories.
        // =====================================================
        [HttpGet("admin/all")]
        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> GetAllCategoriesAdmin()
        {
            var categories = await _context.InterviewCategories
                .AsNoTracking()
                .OrderBy(c => c.Id)
                .Select(c => new
                {
                    c.Id,
                    c.Name,
                    c.Description,
                    c.IsActive
                })
                .ToListAsync();

            return Ok(categories);
        }


        // =====================================================
        // POST: api/InterviewCategories/admin
        // Administrator: Create a new interview category.
        // =====================================================
        [HttpPost("admin")]
        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> CreateCategoryAdmin(
            [FromBody] CreateInterviewCategoryRequest request)
        {
            var name = request.Name.Trim();
            var description = request.Description.Trim();

            if (string.IsNullOrWhiteSpace(name))
            {
                return BadRequest(new
                {
                    message = "Category name is required."
                });
            }

            if (string.IsNullOrWhiteSpace(description))
            {
                return BadRequest(new
                {
                    message = "Category description is required."
                });
            }

            // Prevent duplicate category names.
            var categoryExists = await _context.InterviewCategories
                .AnyAsync(c => c.Name.ToLower() == name.ToLower());

            if (categoryExists)
            {
                return Conflict(new
                {
                    message = "An interview category with this name already exists."
                });
            }

            var category = new InterviewCategory
            {
                Name = name,
                Description = description,
                IsActive = request.IsActive
            };

            _context.InterviewCategories.Add(category);

            await _context.SaveChangesAsync();

            return StatusCode(
                StatusCodes.Status201Created,
                new
                {
                    category.Id,
                    category.Name,
                    category.Description,
                    category.IsActive
                });
        }


        // =====================================================
        // PUT: api/InterviewCategories/admin/{id}
        // Administrator: Update an interview category.
        // =====================================================
        [HttpPut("admin/{id:int}")]
        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> UpdateCategoryAdmin(
            int id,
            [FromBody] UpdateInterviewCategoryRequest request)
        {
            var category = await _context.InterviewCategories
                .FirstOrDefaultAsync(c => c.Id == id);

            if (category == null)
            {
                return NotFound(new
                {
                    message = "Interview category not found."
                });
            }

            var name = request.Name?.Trim();
            var description = request.Description?.Trim();

            if (string.IsNullOrWhiteSpace(name) ||
                string.IsNullOrWhiteSpace(description))
            {
                return BadRequest(new
                {
                    message = "Category name and description are required."
                });
            }

            // Prevent duplicate names across other categories.
            var duplicateExists = await _context.InterviewCategories
                .AnyAsync(c =>
                    c.Id != id &&
                    c.Name.ToLower() == name.ToLower());

            if (duplicateExists)
            {
                return Conflict(new
                {
                    message = "Another interview category already uses this name."
                });
            }

            category.Name = name;
            category.Description = description;
            category.IsActive = request.IsActive;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                category.Id,
                category.Name,
                category.Description,
                category.IsActive
            });
        }


    }
}
