
namespace TsebeCareerPrep.Mvc.Models
{
    public class AdminUserViewModel
    {
        public string Id { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string UserName { get; set; } = string.Empty;

        public List<string> Roles { get; set; } = new();

        public bool IsLockedOut { get; set; }
    }
}
