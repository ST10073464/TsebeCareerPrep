
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

        // Existing category relationship
        public int InterviewCategoryId { get; set; }

        // Display name of the category
        public string InterviewCategoryName { get; set; }
            = string.Empty;
    }
}
