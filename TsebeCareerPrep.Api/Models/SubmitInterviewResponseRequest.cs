
using System.ComponentModel.DataAnnotations;

namespace TsebeCareerPrep.Api.Models
{
    public class SubmitInterviewResponseRequest
    {
        [Range(1, int.MaxValue,
            ErrorMessage = "A valid interview session is required.")]
        public int InterviewSessionId { get; set; }

        [Range(1, int.MaxValue,
            ErrorMessage = "A valid interview question is required.")]
        public int InterviewQuestionId { get; set; }

        [Required(ErrorMessage = "Please provide your answer.")]
        [StringLength(5000, MinimumLength = 1)]
        public string AnswerText { get; set; } = string.Empty;
    }
}
