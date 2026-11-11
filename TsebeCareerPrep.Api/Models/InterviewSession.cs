
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace TsebeCareerPrep.Api.Models
{
    public class InterviewSession
    {
        public int Id { get; set; }

        [Required]
        public int InterviewCategoryId { get; set; }

        public InterviewCategory? InterviewCategory { get; set; }

        // Registered user who owns this session.
        [Required]
        public string UserId { get; set; } = string.Empty;

        // Relationship to ASP.NET Core Identity.
        public IdentityUser? User { get; set; }

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
