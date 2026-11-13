
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
    }
}
