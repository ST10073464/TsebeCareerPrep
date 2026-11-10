
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Json;
using TsebeCareerPrep.Mvc.Models;

namespace TsebeCareerPrep.Mvc.Controllers
{
    public class InterviewQuestionsController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public InterviewQuestionsController(
            IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        // GET: /InterviewQuestions/Category/1
        public async Task<IActionResult> Category(int id)
        {
            var client = _httpClientFactory.CreateClient();

            client.BaseAddress =
                new Uri("https://localhost:7101/");

            var response = await client.GetAsync(
                $"api/InterviewQuestions/category/{id}");

            if (response.StatusCode ==
                System.Net.HttpStatusCode.NotFound)
            {
                return NotFound();
            }

            response.EnsureSuccessStatusCode();

            var questions = await response.Content
                .ReadFromJsonAsync<List<InterviewQuestionViewModel>>();

            ViewBag.CategoryId = id;

            return View(questions ??
                new List<InterviewQuestionViewModel>());
        }
    }
}
