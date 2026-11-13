
using System.ComponentModel.DataAnnotations;

namespace TsebeCareerPrep.Mvc.Models
{
    public class EditInterviewQuestionViewModel
    {
        [Range(1, int.MaxValue)]
        public int Id { get; set; }

        [Range(1, int.MaxValue,
            ErrorMessage = "Please select an interview category.")]
        [Display(Name = "Interview Category")]
        public int InterviewCategoryId { get; set; }

        [Required(ErrorMessage = "Please enter a question.")]
        [StringLength(1000, MinimumLength = 5)]
        [Display(Name = "Question")]
        public string QuestionText { get; set; } = string.Empty;

        [StringLength(2000)]
        [Display(Name = "Suggested Answer")]
        public string? SuggestedAnswer { get; set; }

        [Required(ErrorMessage = "Please select a difficulty level.")]
        [StringLength(30)]
        [Display(Name = "Difficulty Level")]
        public string DifficultyLevel { get; set; } = "Beginner";

        [Display(Name = "Active Question")]
        public bool IsActive { get; set; } = true;
    }
}
