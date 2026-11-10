
using System.ComponentModel.DataAnnotations;

namespace TsebeCareerPrep.Api.Models
{
    public class InterviewResponse
    {
        public int Id { get; set; }

        [Required]
        public int InterviewSessionId { get; set; }

        public InterviewSession? InterviewSession { get; set; }

        [Required]
        public int InterviewQuestionId { get; set; }

        public InterviewQuestion? InterviewQuestion { get; set; }

        [Required]
        [MaxLength(5000)]
        public string AnswerText { get; set; } = string.Empty;

        public DateTime SubmittedAt { get; set; }
            = DateTime.UtcNow;
    }
}
