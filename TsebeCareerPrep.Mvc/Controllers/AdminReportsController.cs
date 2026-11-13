
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TsebeCareerPrep.Mvc.Models;

namespace TsebeCareerPrep.Mvc.Controllers
{
    [Authorize(Roles = "Administrator")]
    public class AdminReportsController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public AdminReportsController(
            IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        // =====================================================
        // GET: /AdminReports
        // Administrator reporting dashboard.
        // =====================================================
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var client = _httpClientFactory.CreateClient(
                "AuthenticatedInterviewApi");

            HttpResponseMessage response;

            try
            {
                response = await client.GetAsync(
                    "api/AdminReports/overview");
            }
            catch (HttpRequestException)
            {
                return StatusCode(
                    503,
                    "The reporting API is currently unavailable.");
            }
            catch (TaskCanceledException)
            {
                return StatusCode(
                    504,
                    "The reporting API request timed out.");
            }

            using (response)
            {
                if (response.StatusCode == HttpStatusCode.Unauthorized)
                    return Challenge();

                if (response.StatusCode == HttpStatusCode.Forbidden)
                    return Forbid();

                if (!response.IsSuccessStatusCode)
                {
                    return StatusCode(
                        (int)response.StatusCode,
                        "Unable to retrieve administrator reports.");
                }

                AdminReportOverviewViewModel? report;

                try
                {
                    report = await response.Content
                        .ReadFromJsonAsync<AdminReportOverviewViewModel>();
                }
                catch (JsonException)
                {
                    return StatusCode(
                        502,
                        "The reporting API returned invalid data.");
                }

                if (report == null)
                {
                    return StatusCode(
                        502,
                        "The reporting API returned an empty response.");
                }

                return View(report);
            }
        }
    }
}
