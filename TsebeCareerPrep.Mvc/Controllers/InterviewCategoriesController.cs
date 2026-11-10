
using Microsoft.AspNetCore.Mvc;
using TsebeCareerPrep.Mvc.Services;

namespace TsebeCareerPrep.Mvc.Controllers
{
    public class InterviewCategoriesController : Controller
    {
        private readonly InterviewCategoryService _categoryService;

        public InterviewCategoriesController(
            InterviewCategoryService categoryService)
        {
            _categoryService = categoryService;
        }

        public async Task<IActionResult> Index()
        {
            var categories = await _categoryService.GetCategoriesAsync();

            return View(categories);
        }
    }
}
