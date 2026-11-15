
namespace TsebeCareerPrep.Api.Services
{
    public class InterviewAssessmentFeedback
    {
        public int InterviewResponseId { get; set; }

        public int InterviewQuestionId { get; set; }

        public List<string> DetectedConcepts { get; set; }
            = new();

        public List<string> ConceptsToReview { get; set; }
            = new();

        public string ImprovementGuidance { get; set; }
            = string.Empty;

        // Indicative concept-coverage percentage (0–100).
        // This is not a formal 10-mark assessment.
        public int? ConceptCoveragePercentage { get; set; }

        // Potential misunderstandings identified by
        // question-specific assessment rules.
        // These are warnings, not verified errors.
        public List<string> PotentialMisconceptions { get; set; }
            = new();

        // Indicates whether any misconception rule matched.
        public bool HasPotentialMisconceptions =>
            PotentialMisconceptions.Count > 0;

        public bool RequiresManualReview { get; set; } = true;

        public string ReviewReason { get; set; }
            = string.Empty;

        public string AssessmentStatus { get; set; }
            = "Not formally scored";
    }
}
