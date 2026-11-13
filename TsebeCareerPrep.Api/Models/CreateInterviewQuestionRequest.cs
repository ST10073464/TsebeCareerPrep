
using System.ComponentModel.DataAnnotations;

namespace TsebeCareerPrep.Api.Models
{
    public class CreateInterviewQuestionRequest
    {
        [Range(1, int.MaxValue,
            ErrorMessage = "A valid interview category is required.")]
        public int InterviewCategoryId { get; set; }

        [Required]
        [StringLength(1000, MinimumLength = 5)]
        public string QuestionText { get; set; } = string.Empty;

        [MaxLength(2000)]
        public string? SuggestedAnswer { get; set; }

        [Required]
        [StringLength(30)]
        public string DifficultyLevel { get; set; } = "Beginner";

        public bool IsActive { get; set; } = true;
    }
}
