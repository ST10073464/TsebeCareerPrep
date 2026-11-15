
namespace TsebeCareerPrep.Mvc.Models
{
    public class InterviewResultsViewModel
    {
        public InterviewSessionViewModel Session { get; set; } = new();

        public int TotalAnswered { get; set; }

        public List<InterviewResultAnswerViewModel> Responses { get; set; } = new();

        public int TotalQuestions { get; set; }

        public int TotalSkipped =>
            Math.Max(0, TotalQuestions - TotalAnswered);

        public List<InterviewResultQuestionViewModel> Questions { get; set; } = new();
    }

    public class InterviewResultAnswerViewModel
    {
        public int InterviewQuestionId { get; set; }

        public string QuestionText { get; set; } = string.Empty;

        public string DifficultyLevel { get; set; } = string.Empty;

        public string AnswerText { get; set; } = string.Empty;

        public string SuggestedAnswer { get; set; } = string.Empty;

        public DateTime SubmittedAt { get; set; }

        // Feedback returned by the API.
        public InterviewAssessmentFeedbackViewModel?
            AssessmentFeedback
        { get; set; }
    }

    // Represents all questions, including skipped questions.
    public class InterviewResultQuestionViewModel
    {
        public int QuestionNumber { get; set; }

        public int InterviewQuestionId { get; set; }

        public string QuestionText { get; set; } = string.Empty;

        public string DifficultyLevel { get; set; } = string.Empty;

        public bool IsAnswered { get; set; }

        public string AnswerText { get; set; } = string.Empty;

        public string SuggestedAnswer { get; set; } = string.Empty;

        public DateTime? SubmittedAt { get; set; }

        public int Id { get; set; }

        // Null for skipped questions.
        public InterviewAssessmentFeedbackViewModel?
            AssessmentFeedback
        { get; set; }
    }

    // Indicative feedback, not a formally awarded score.
    public class InterviewAssessmentFeedbackViewModel
    {
        public List<string> DetectedConcepts { get; set; } = new();

        public List<string> ConceptsToReview { get; set; } = new();

        public string ImprovementGuidance { get; set; } = string.Empty;

        // Calculated by the API from detected expected concepts.
        // Nullable to support older responses without this value.
        public int? ConceptCoveragePercentage { get; set; }

        // Potential misconceptions identified by the API.
        // These are warnings, not verified technical errors.
        public List<string> PotentialMisconceptions { get; set; }
            = new();

        // True when at least one misconception warning exists.
        public bool HasPotentialMisconceptions =>
            PotentialMisconceptions?.Count > 0;

        public bool RequiresManualReview { get; set; } = true;

        public string ReviewReason { get; set; } = string.Empty;

        public string AssessmentStatus { get; set; }
            = "Not formally scored";
    }
}
