namespace TsebeCareerPrep.Api.Models
{
    public class UserAdminAction
    {
        public int Id { get; set; }
        public string TargetUserId { get; set; } = string.Empty;
        public string TargetEmail { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public string PerformedByUserId { get; set; } = string.Empty;
        public DateTime OccurredUtc { get; set; }
    }
}
