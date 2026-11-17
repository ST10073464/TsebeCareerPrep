
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TsebeCareerPrep.Api.Data;

namespace TsebeCareerPrep.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Administrator,SuperAdministrator")]
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

            var promotedAdministrators = await _context.UserAdminActions.CountAsync(a => a.Action == "Promoted");
            var demotedAdministrators = await _context.UserAdminActions.CountAsync(a => a.Action == "Demoted");
            var deletedAdministrators = await _context.UserAdminActions.CountAsync(a => a.Action == "Deleted");
            var restoredAccounts = await _context.UserAdminActions.CountAsync(a => a.Action == "Restored");
            var recentUserAdminActions = await (
                from action in _context.UserAdminActions.AsNoTracking()
                join performer in _context.Users.AsNoTracking()
                    on action.PerformedByUserId equals performer.Id into performers
                from performer in performers.DefaultIfEmpty()
                orderby action.OccurredUtc descending
                select new
                {
                    action.TargetEmail,
                    action.Action,
                    action.OccurredUtc,
                    PerformedBy = performer == null ? "System" : performer.Email
                })
                .Take(20)
                .ToListAsync();

            return Ok(new
            {
                TotalUsers = totalUsers,
                TotalSessions = totalSessions,
                CompletedSessions = completedSessions,
                InProgressSessions = inProgressSessions,
                ActiveCategories = activeCategories,
                TotalAnswers = totalAnswers,
                CompletionRate = completionRate,
                PromotedAdministrators = promotedAdministrators,
                DemotedAdministrators = demotedAdministrators,
                DeletedAdministrators = deletedAdministrators,
                RestoredAccounts = restoredAccounts,
                RecentUserAdminActions = recentUserAdminActions
            });
        }
    }
}
