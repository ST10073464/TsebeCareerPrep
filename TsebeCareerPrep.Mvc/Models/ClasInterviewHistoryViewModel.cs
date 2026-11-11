
namespace TsebeCareerPrep.Mvc.Models
{
    public class InterviewHistoryViewModel
    {
        public int Id { get; set; }

        public int InterviewCategoryId { get; set; }

        public string CategoryName { get; set; }
            = string.Empty;

        public string Status { get; set; }
            = string.Empty;

        public DateTime StartedAt { get; set; }

        public DateTime? CompletedAt { get; set; }

        public int TotalAnswered { get; set; }
    }
}
