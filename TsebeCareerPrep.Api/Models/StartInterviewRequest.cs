
using System.ComponentModel.DataAnnotations;

namespace TsebeCareerPrep.Api.Models
{
    public class StartInterviewRequest
    {
        [Range(1, int.MaxValue,
            ErrorMessage = "Please select a valid interview category.")]
        public int InterviewCategoryId { get; set; }
    }
}
