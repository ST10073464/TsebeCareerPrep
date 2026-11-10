
namespace TsebeCareerPrep.Mvc.Models
{
    public class InterviewSessionQuestionViewModel
    {
        public int Id { get; set; }

        public int InterviewCategoryId { get; set; }

        public string QuestionText { get; set; } = string.Empty;

        public string DifficultyLevel { get; set; } = string.Empty;

        public bool IsAnswered { get; set; }
    }
}
