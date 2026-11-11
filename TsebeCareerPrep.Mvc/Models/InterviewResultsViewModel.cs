namespace TsebeCareerPrep.Mvc.Models
{
    public class InterviewResultsViewModel
    {
        public InterviewSessionViewModel Session { get; set; } = new();

        public int TotalAnswered { get; set; }

        public List<InterviewResultAnswerViewModel> Responses { get; set; } = new();

        public int TotalQuestions { get; set; }

        public int TotalSkipped => Math.Max(0, TotalQuestions - TotalAnswered);

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

        // Feedback returned alongside the submitted answer by the API.
        public InterviewAssessmentFeedbackViewModel? AssessmentFeedback { get; set; }
    }

    // Represents every question, including skipped questions.
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

        // Populated from the matching submitted answer; null for skipped questions.
        public InterviewAssessmentFeedbackViewModel? AssessmentFeedback { get; set; }
    }

    // Indicative learning feedback; does not represent a formally awarded score.
    public class InterviewAssessmentFeedbackViewModel
    {
        public List<string> DetectedConcepts { get; set; } = new();

        public List<string> ConceptsToReview { get; set; } = new();

        public string ImprovementGuidance { get; set; } = string.Empty;

        public bool RequiresManualReview { get; set; } = true;

        public string ReviewReason { get; set; } = string.Empty;

        public string AssessmentStatus { get; set; } = "Not formally scored";
    }
}
