
using System.ComponentModel.DataAnnotations;

namespace TsebeCareerPrep.Api.Models
{
    public class ProvisionalInterviewAssessment
    {
        [Range(0, 10)]
        public int SuggestedScore { get; set; }

        [Range(0, 4)]
        public int TechnicalAccuracyScore { get; set; }

        [Range(0, 3)]
        public int KeyConceptScore { get; set; }

        [Range(0, 2)]
        public int ClarityScore { get; set; }

        [Range(0, 1)]
        public int PracticalExampleScore { get; set; }

        [MaxLength(2000)]
        public string Strengths { get; set; }
            = string.Empty;

        [MaxLength(2000)]
        public string AreasForImprovement { get; set; }
            = string.Empty;

        [MaxLength(2000)]
        public string ImprovementGuidance { get; set; }
            = string.Empty;

        public bool RequiresManualReview { get; set; } = true;
    }
}
