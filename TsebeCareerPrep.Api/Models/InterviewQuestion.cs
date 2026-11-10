
using System.ComponentModel.DataAnnotations;

namespace TsebeCareerPrep.Api.Models
{
    public class InterviewQuestion
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(1000)]
        public string QuestionText { get; set; } = string.Empty;

        [MaxLength(2000)]
        public string? SuggestedAnswer { get; set; }

        [Required]
        [MaxLength(30)]
        public string DifficultyLevel { get; set; } = "Beginner";

        public bool IsActive { get; set; } = true;

        public int InterviewCategoryId { get; set; }

        public InterviewCategory? InterviewCategory { get; set; }
    }
}

