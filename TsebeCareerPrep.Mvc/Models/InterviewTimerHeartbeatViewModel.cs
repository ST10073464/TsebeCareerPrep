
using System;

namespace TsebeCareerPrep.Mvc.Models
{
    public class InterviewTimerHeartbeatViewModel
    {
        public int Id { get; set; }

        public string Status { get; set; } = string.Empty;

        public bool IsTimerRunning { get; set; }

        public long ActiveDurationSeconds { get; set; }

        public DateTime? LastTimerHeartbeatUtc { get; set; }
    }
}
