
using System.ComponentModel.DataAnnotations;

namespace TsebeCareerPrep.Mvc.Models
{
    public class AdminAssessmentRubricViewModel
    {
        public int InterviewQuestionId { get; set; }

        public string QuestionText { get; set; } = string.Empty;

        public int CategoryId { get; set; }

        [Required]
        [MaxLength(2000)]
        public string ExpectedConcepts { get; set; } = string.Empty;

        [MaxLength(2000)]
        public string AcceptableAlternatives { get; set; } = string.Empty;

        [MaxLength(2000)]
        public string AssessmentGuidance { get; set; } = string.Empty;

        [MaxLength(2000)]
        public string CandidateGuidance { get; set; } = string.Empty;

        [Range(0, 10)]
        public int TechnicalAccuracyMarks { get; set; } = 4;

        [Range(0, 10)]
        public int KeyConceptMarks { get; set; } = 3;

        [Range(0, 10)]
        public int ClarityMarks { get; set; } = 2;

        [Range(0, 10)]
        public int PracticalExampleMarks { get; set; } = 1;

        public bool IsActive { get; set; } = true;
    }
}
