
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TsebeCareerPrep.Api.Data;

namespace TsebeCareerPrep.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Administrator")]
    public class AdminReportsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public AdminReportsController(
            ApplicationDbContext context)
        {
            _context = context;
        }

        // =====================================================
        // GET: api/AdminReports/overview
        // Administrator-only platform statistics.
        // =====================================================
        [HttpGet("overview")]
        public async Task<IActionResult> GetOverview()
        {
            var totalUsers = await _context.Users.CountAsync();

            var totalSessions = await _context.InterviewSessions
                .CountAsync();

            var completedSessions = await _context.InterviewSessions
                .CountAsync(s => s.Status == "Completed");

            var inProgressSessions = await _context.InterviewSessions
                .CountAsync(s => s.Status == "InProgress");

            var activeCategories = await _context.InterviewCategories
                .CountAsync(c => c.IsActive);

            var totalAnswers = await _context.InterviewResponses
                .CountAsync();

            var completionRate = totalSessions > 0
                ? Math.Round(
                    (double)completedSessions / totalSessions * 100,
                    1)
                : 0;

            return Ok(new
            {
                TotalUsers = totalUsers,
                TotalSessions = totalSessions,
                CompletedSessions = completedSessions,
                InProgressSessions = inProgressSessions,
                ActiveCategories = activeCategories,
                TotalAnswers = totalAnswers,
                CompletionRate = completionRate
            });
        }
    }
}
