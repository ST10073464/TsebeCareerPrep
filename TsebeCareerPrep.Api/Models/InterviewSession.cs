
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace TsebeCareerPrep.Api.Models
{
    public class InterviewSession
    {
        public int Id { get; set; }

        [Required]
        public int InterviewCategoryId { get; set; }

        public InterviewCategory? InterviewCategory { get; set; }

        // Registered user who owns this session.
        [Required]
        public string UserId { get; set; } = string.Empty;

        // Relationship to ASP.NET Core Identity.
        public IdentityUser? User { get; set; }

        public DateTime StartedAt { get; set; }
            = DateTime.UtcNow;

        public DateTime? CompletedAt { get; set; }

        [Required]
        [MaxLength(30)]
        public string Status { get; set; } = "InProgress";

        // Accumulated active interview time in seconds.
        // This excludes time when the interview is paused.
        public long ActiveDurationSeconds { get; set; } = 0;

        // UTC timestamp of the most recent timer activation.
        // Null means the timer is currently paused.
        public DateTime? TimerStartedAtUtc { get; set; }

        // Indicates whether the interview timer is running.
        public bool IsTimerRunning { get; set; } = false;

        // UTC timestamp of the last timer heartbeat.
        // Used to prevent abandoned sessions from
        // accumulating unlimited inactive time.
        public DateTime? LastTimerHeartbeatUtc { get; set; }

        public ICollection<InterviewResponse> Responses { get; set; }
            = new List<InterviewResponse>();

        public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    }
}
