
using System;

namespace TsebeCareerPrep.Api.Models
{
    public class InterviewMisconceptionRule
    {
        public int Id { get; set; }

        // The interview question this rule belongs to.
        public int InterviewQuestionId { get; set; }

        public InterviewQuestion InterviewQuestion { get; set; }
            = null!;

        // Phrase that may indicate a misconception.
        public string TriggerPhrase { get; set; }
            = string.Empty;

        // Explanation shown when the phrase is detected.
        public string FeedbackMessage { get; set; }
            = string.Empty;

        // Allows administrators to disable a rule
        // without deleting it.
        public bool IsActive { get; set; } = true;

        // For future administrator management and auditing.
        public DateTime CreatedAt { get; set; }
            = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }
    }
}
