
namespace TsebeCareerPrep.Mvc.Models
{
    public class InterviewQuestionViewModel
    {
        public int Id { get; set; }

        public string QuestionText { get; set; }
            = string.Empty;

        public string? SuggestedAnswer { get; set; }

        public string DifficultyLevel { get; set; }
            = string.Empty;

        public bool IsActive { get; set; }

        public int InterviewCategoryId { get; set; }
    }
}
