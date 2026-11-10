
using System.ComponentModel.DataAnnotations;

namespace TsebeCareerPrep.Api.Models
{
    public class InterviewSession
    {
        public int Id { get; set; }

        [Required]
        public int InterviewCategoryId { get; set; }

        public InterviewCategory? InterviewCategory { get; set; }

        public DateTime StartedAt { get; set; }
            = DateTime.UtcNow;

        public DateTime? CompletedAt { get; set; }

        [Required]
        [MaxLength(30)]
        public string Status { get; set; } = "InProgress";

        public ICollection<InterviewResponse> Responses { get; set; }
            = new List<InterviewResponse>();
    }
}
