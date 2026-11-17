namespace TsebeCareerPrep.Api.Models
{
    // Keeps the Identity account and its original ID while access is disabled.
    public class DeactivatedUser
    {
        public string UserId { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public DateTime DeactivatedUtc { get; set; }
    }
}
