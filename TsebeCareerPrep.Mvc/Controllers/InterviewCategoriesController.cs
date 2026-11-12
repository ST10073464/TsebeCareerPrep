
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Json;
using TsebeCareerPrep.Mvc.Models;
using TsebeCareerPrep.Mvc.Services;

namespace TsebeCareerPrep.Mvc.Controllers
{
    public class InterviewCategoriesController : Controller
    {
        private readonly InterviewCategoryService _categoryService;
        private readonly IHttpClientFactory _httpClientFactory;

        public InterviewCategoriesController(
            InterviewCategoryService categoryService,
            IHttpClientFactory httpClientFactory)
        {
            _categoryService = categoryService;
            _httpClientFactory = httpClientFactory;
        }

        public async Task<IActionResult> Index()
        {
            var categories =
                await _categoryService.GetCategoriesAsync();

            if (User.Identity?.IsAuthenticated == true)
            {
                var client = _httpClientFactory.CreateClient(
                    "AuthenticatedInterviewApi");

                var response = await client.GetAsync(
                    "api/InterviewSessions/history");

                if (!response.IsSuccessStatusCode)
                {
                    return StatusCode(
                        502,
                        "Unable to retrieve your interview history.");
                }

                var history = await response.Content
                    .ReadFromJsonAsync<List<InterviewHistoryViewModel>>()
                    ?? new List<InterviewHistoryViewModel>();

                ViewBag.InterviewHistory = history;
            }
            else
            {
                ViewBag.InterviewHistory =
                    new List<InterviewHistoryViewModel>();
            }

            return View(categories);
        }

        // GET: /InterviewCategories/Details/1
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            if (id <= 0)
            {
                return BadRequest(
                    "Invalid interview category ID.");
            }

            var categories =
                await _categoryService.GetCategoriesAsync();

            var category = categories.FirstOrDefault(
                c => c.Id == id);

            if (category == null)
            {
                return NotFound(
                    "Interview category not found.");
            }

            var history = new List<InterviewHistoryViewModel>();

            if (User.Identity?.IsAuthenticated == true)
            {
                var client = _httpClientFactory.CreateClient(
                    "AuthenticatedInterviewApi");

                var response = await client.GetAsync(
                    "api/InterviewSessions/history");

                if (!response.IsSuccessStatusCode)
                {
                    return StatusCode(
                        502,
                        "Unable to retrieve your interview history.");
                }

                var allHistory = await response.Content
                    .ReadFromJsonAsync<List<InterviewHistoryViewModel>>()
                    ?? new List<InterviewHistoryViewModel>();

                history = allHistory
                    .Where(h => h.InterviewCategoryId == id)
                    .OrderByDescending(h => h.StartedAt)
                    .ThenByDescending(h => h.Id)
                    .ToList();
            }

            ViewBag.InterviewHistory = history;

            return View(category);
        }
    }
}
