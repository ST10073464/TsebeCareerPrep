
using TsebeCareerPrep.Api.Models;

namespace TsebeCareerPrep.Api.Services
{
    public static class AiAssessmentValidationService
    {
        public static bool IsValid(
            ProvisionalInterviewAssessment assessment,
            InterviewQuestionRubric rubric)
        {
            if (assessment == null || rubric == null)
                return false;

            int totalAvailable =
                rubric.TechnicalAccuracyMarks +
                rubric.KeyConceptMarks +
                rubric.ClarityMarks +
                rubric.PracticalExampleMarks;

            if (rubric.TechnicalAccuracyMarks < 0 ||
                rubric.KeyConceptMarks < 0 ||
                rubric.ClarityMarks < 0 ||
                rubric.PracticalExampleMarks < 0 ||
                totalAvailable != 10)
            {
                return false;
            }

            if (assessment.TechnicalAccuracyScore < 0 ||
                assessment.TechnicalAccuracyScore >
                    rubric.TechnicalAccuracyMarks)
                return false;

            if (assessment.KeyConceptScore < 0 ||
                assessment.KeyConceptScore >
                    rubric.KeyConceptMarks)
                return false;

            if (assessment.ClarityScore < 0 ||
                assessment.ClarityScore >
                    rubric.ClarityMarks)
                return false;

            if (assessment.PracticalExampleScore < 0 ||
                assessment.PracticalExampleScore >
                    rubric.PracticalExampleMarks)
                return false;

            int calculatedTotal =
                assessment.TechnicalAccuracyScore +
                assessment.KeyConceptScore +
                assessment.ClarityScore +
                assessment.PracticalExampleScore;

            if (assessment.SuggestedScore != calculatedTotal)
                return false;

            if (assessment.SuggestedScore < 0 ||
                assessment.SuggestedScore > 10)
                return false;

            if (string.IsNullOrWhiteSpace(
                assessment.ImprovementGuidance))
                return false;

            if (assessment.Strengths.Length > 2000 ||
                assessment.AreasForImprovement.Length > 2000 ||
                assessment.ImprovementGuidance.Length > 2000)
                return false;

            return true;
        }
    }
}
