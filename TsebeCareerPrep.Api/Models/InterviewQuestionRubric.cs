
using System.ComponentModel.DataAnnotations;

namespace TsebeCareerPrep.Api.Models
{
    public class InterviewQuestionRubric
    {
        public int Id { get; set; }

        [Required]
        public int InterviewQuestionId { get; set; }

        public InterviewQuestion? InterviewQuestion { get; set; }

        [Required]
        [MaxLength(2000)]
        public string ExpectedConcepts { get; set; }
            = string.Empty;

        [MaxLength(2000)]
        public string AcceptableAlternatives { get; set; }
            = string.Empty;

        [MaxLength(2000)]
        public string AssessmentGuidance { get; set; }
            = string.Empty;

        public int TechnicalAccuracyMarks { get; set; } = 4;

        public int KeyConceptMarks { get; set; } = 3;

        public int ClarityMarks { get; set; } = 2;

        public int PracticalExampleMarks { get; set; } = 1;

        public bool IsActive { get; set; } = true;
    }
}
