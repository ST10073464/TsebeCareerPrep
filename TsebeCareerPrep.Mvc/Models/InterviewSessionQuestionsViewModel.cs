
using System;
using System.Collections.Generic;

namespace TsebeCareerPrep.Mvc.Models
{
    public class InterviewSessionQuestionsViewModel
    {
        public int SessionId { get; set; }

        public int InterviewCategoryId { get; set; }

        public string CategoryName { get; set; }
            = string.Empty;

        public string Status { get; set; }
            = string.Empty;

        // Interview timer properties

        public long ActiveDurationSeconds { get; set; } = 0;

        public bool IsTimerRunning { get; set; } = false;

        public DateTime? TimerStartedAtUtc { get; set; }

        public DateTime? LastTimerHeartbeatUtc { get; set; }

        // Interview questions

        public List<InterviewSessionQuestionViewModel> Questions
        { get; set; } = new();
    }
}
