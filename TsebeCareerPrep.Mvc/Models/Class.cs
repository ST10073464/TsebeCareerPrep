
using System.Collections.Generic;

namespace TsebeCareerPrep.Mvc.Models
{
    public class InterviewSessionQuestionsViewModel
    {
        public int SessionId { get; set; }

        public int InterviewCategoryId { get; set; }

        public string CategoryName { get; set; }
            = string.Empty;

        public string Status { get; set; }
            = string.Empty;

        public List<InterviewSessionQuestionViewModel> Questions
        { get; set; } = new();
    }
}
