
namespace TsebeCareerPrep.Api.Services
{
    public class InterviewAssessmentEvidence
    {
        public int InterviewResponseId { get; set; }

        public int InterviewQuestionId { get; set; }

        public List<string> DetectedConcepts { get; set; }
            = new();

        public List<string> MissingConcepts { get; set; }
            = new();

        public bool RequiresManualReview { get; set; } = true;

        public string ReviewReason { get; set; } =
            "Technical accuracy and understanding require review.";
    }
}
