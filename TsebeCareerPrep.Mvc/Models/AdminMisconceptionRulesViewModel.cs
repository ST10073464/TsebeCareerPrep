
using System.ComponentModel.DataAnnotations;

namespace TsebeCareerPrep.Mvc.Models
{
    public class AdminMisconceptionRulesViewModel
    {
        public int QuestionId { get; set; }
        public string QuestionText { get; set; } = "";
        public int CategoryId { get; set; }

        public List<AdminMisconceptionRuleItem> Rules { get; set; }
            = new();
    }

    public class AdminMisconceptionRuleItem
    {
        public int Id { get; set; }
        public int InterviewQuestionId { get; set; }
        public string TriggerPhrase { get; set; } = "";
        public string FeedbackMessage { get; set; } = "";
        public bool IsActive { get; set; }
    }

    public class AdminMisconceptionRuleInput
    {
        [Range(1, int.MaxValue)]
        public int InterviewQuestionId { get; set; }

        [Required]
        [StringLength(500)]
        public string TriggerPhrase { get; set; } = "";

        [Required]
        [StringLength(2000)]
        public string FeedbackMessage { get; set; } = "";

        public bool IsActive { get; set; } = true;
    }
}
