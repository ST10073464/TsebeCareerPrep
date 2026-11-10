
using Microsoft.AspNetCore.Mvc;
using TsebeCareerPrep.Api.Models;

namespace TsebeCareerPrep.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class InterviewCategoriesController : ControllerBase
    {
        private static readonly List<InterviewCategory> Categories = new()
        {
            new InterviewCategory
            {
                Id = 1,
                Name = "Software Development",
                Description = "Technical and programming interviews"
            },
            new InterviewCategory
            {
                Id = 2,
                Name = "Administration",
                Description = "Office administration and support interviews"
            },
            new InterviewCategory
            {
                Id = 3,
                Name = "Finance",
                Description = "Accounting and financial management interviews"
            },
            new InterviewCategory
            {
                Id = 4,
                Name = "Management",
                Description = "Leadership and management interviews"
            }
        };

        [HttpGet]
        public ActionResult<IEnumerable<InterviewCategory>> GetCategories()
        {
            return Ok(Categories.Where(c => c.IsActive));
        }

        [HttpGet("{id:int}")]
        public ActionResult<InterviewCategory> GetCategory(int id)
        {
            var category = Categories.FirstOrDefault(
                c => c.Id == id && c.IsActive);

            if (category == null)
                return NotFound();

            return Ok(category);
        }
    }
}
