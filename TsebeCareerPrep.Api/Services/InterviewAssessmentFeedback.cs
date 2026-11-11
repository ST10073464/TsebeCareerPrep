
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

        public bool RequiresManualReview { get; set; } = true;

        public string ReviewReason { get; set; }
            = string.Empty;

        public string AssessmentStatus { get; set; }
            = "Not formally scored";
    }
}
