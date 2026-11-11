
namespace TsebeCareerPrep.Mvc.Models
{
    public class InterviewProgressViewModel
    {
        public int TotalInterviews { get; set; }

        public int CompletedInterviews { get; set; }

        public int InProgressInterviews { get; set; }

        public int TotalAnswers { get; set; }

        public double CompletionRate { get; set; }
    }
}
