
namespace TsebeCareerPrep.Mvc.Models
{
    public class AdminReportOverviewViewModel
    {
        public int TotalUsers { get; set; }

        public int TotalSessions { get; set; }

        public int CompletedSessions { get; set; }

        public int InProgressSessions { get; set; }

        public int ActiveCategories { get; set; }

        public int TotalAnswers { get; set; }

        public double CompletionRate { get; set; }
        public int PromotedAdministrators { get; set; }
        public int DemotedAdministrators { get; set; }
        public int DeletedAdministrators { get; set; }
        public int RestoredAccounts { get; set; }
        public List<AdminUserActionViewModel> RecentUserAdminActions { get; set; } = new();
    }

    public class AdminUserActionViewModel
    {
        public string TargetEmail { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public DateTime OccurredUtc { get; set; }
        public string? PerformedBy { get; set; }
    }
}
