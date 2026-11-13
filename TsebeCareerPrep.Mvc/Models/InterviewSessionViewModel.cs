
namespace TsebeCareerPrep.Mvc.Models
{
    public class InterviewSessionViewModel
    {
        public int Id { get; set; }

        public int InterviewCategoryId { get; set; }

        public string CategoryName { get; set; } = string.Empty;

        public DateTime StartedAt { get; set; }

        public DateTime? CompletedAt { get; set; }

        public string Status { get; set; } = string.Empty;

        // Interview timer properties
        public long ActiveDurationSeconds { get; set; } = 0;

        public bool IsTimerRunning { get; set; } = false;

        public DateTime? TimerStartedAtUtc { get; set; }

        public DateTime? LastTimerHeartbeatUtc { get; set; }
    }
}
