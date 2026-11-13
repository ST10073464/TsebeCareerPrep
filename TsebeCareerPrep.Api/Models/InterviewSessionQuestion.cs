
using System;
using System.ComponentModel.DataAnnotations;

namespace TsebeCareerPrep.Api.Models
{
    public class InterviewSessionQuestion
    {
        public int Id { get; set; }

        [Required]
        public int InterviewSessionId { get; set; }

        public InterviewSession? InterviewSession { get; set; }

        [Required]
        public int InterviewQuestionId { get; set; }

        public InterviewQuestion? InterviewQuestion { get; set; }

        [Required]
        [MaxLength(1000)]
        public string QuestionText { get; set; } = string.Empty;

        [MaxLength(2000)]
        public string? SuggestedAnswer { get; set; }

        [Required]
        [MaxLength(30)]
        public string DifficultyLevel { get; set; } = "Beginner";

        // Preserves the original question order.
        public int DisplayOrder { get; set; }

        // Records when the question was assigned.
        public DateTime AssignedAtUtc { get; set; } = DateTime.UtcNow;
    }
}
