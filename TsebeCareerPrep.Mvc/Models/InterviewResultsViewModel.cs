
namespace TsebeCareerPrep.Mvc.Models
{
    public class InterviewResultsViewModel
    {
        public InterviewSessionViewModel Session { get; set; }
            = new();

        public int TotalAnswered { get; set; }

        public List<InterviewResultAnswerViewModel> Responses
        { get; set; } = new();
    }

    public class InterviewResultAnswerViewModel
    {
        public int InterviewQuestionId { get; set; }

        public string QuestionText { get; set; }
            = string.Empty;

        public string DifficultyLevel { get; set; }
            = string.Empty;

        public string AnswerText { get; set; }
            = string.Empty;

        public string SuggestedAnswer { get; set; }
            = string.Empty;

        public DateTime SubmittedAt { get; set; }
    }
}
