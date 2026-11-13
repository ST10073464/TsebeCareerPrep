
using System.ComponentModel.DataAnnotations;

namespace TsebeCareerPrep.Api.Models
{
    public class InterviewAssessment
    {
        public int Id { get; set; }

        [Required]
        public int InterviewResponseId { get; set; }

        public InterviewResponse? InterviewResponse { get; set; }

        // Formal score. Only valid after reviewer approval.
        [Range(0, 10)]
        public int Score { get; set; }

        // AI-suggested score awaiting reviewer approval.
        [Range(0, 10)]
        public int? ProvisionalScore { get; set; }

        [Required]
        [MaxLength(50)]
        public string AssessmentStatus { get; set; }
            = "PendingReview";

        [MaxLength(450)]
        public string? ReviewedByUserId { get; set; }

        public DateTime? ReviewedAt { get; set; }

        [MaxLength(2000)]
        public string Strengths { get; set; }
            = string.Empty;

        [MaxLength(2000)]
        public string AreasForImprovement { get; set; }
            = string.Empty;

        [MaxLength(2000)]
        public string ImprovementGuidance { get; set; }
            = string.Empty;

        [Required]
        [MaxLength(50)]
        public string AssessmentMethod { get; set; }
            = "CriteriaBased";

        public DateTime AssessedAt { get; set; }
            = DateTime.UtcNow;
    }
}
