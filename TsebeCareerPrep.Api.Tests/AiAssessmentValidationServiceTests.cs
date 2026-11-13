
using TsebeCareerPrep.Api.Models;
using TsebeCareerPrep.Api.Services;
using Xunit;

namespace TsebeCareerPrep.Api.Tests
{
    public class AiAssessmentValidationServiceTests
    {
        private static InterviewQuestionRubric CreateRubric()
        {
            return new InterviewQuestionRubric
            {
                ExpectedConcepts =
                    "Accuracy; Concepts; Explanation",
                TechnicalAccuracyMarks = 4,
                KeyConceptMarks = 3,
                ClarityMarks = 2,
                PracticalExampleMarks = 1
            };
        }

        private static ProvisionalInterviewAssessment
            CreateAssessment()
        {
            return new ProvisionalInterviewAssessment
            {
                SuggestedScore = 8,
                TechnicalAccuracyScore = 3,
                KeyConceptScore = 2,
                ClarityScore = 2,
                PracticalExampleScore = 1,
                Strengths = "Clear explanation.",
                AreasForImprovement =
                    "Include additional concepts.",
                ImprovementGuidance =
                    "Provide a detailed practical example."
            };
        }

        [Fact]
        public void ValidAssessment_ReturnsTrue()
        {
            Assert.True(
                AiAssessmentValidationService.IsValid(
                    CreateAssessment(),
                    CreateRubric()));
        }

        [Fact]
        public void ScoreExceedsCriterion_ReturnsFalse()
        {
            var assessment = CreateAssessment();
            assessment.TechnicalAccuracyScore = 5;
            assessment.SuggestedScore = 10;

            Assert.False(
                AiAssessmentValidationService.IsValid(
                    assessment,
                    CreateRubric()));
        }

        [Fact]
        public void IncorrectTotal_ReturnsFalse()
        {
            var assessment = CreateAssessment();
            assessment.SuggestedScore = 9;

            Assert.False(
                AiAssessmentValidationService.IsValid(
                    assessment,
                    CreateRubric()));
        }

        [Fact]
        public void InvalidRubricTotal_ReturnsFalse()
        {
            var rubric = CreateRubric();
            rubric.PracticalExampleMarks = 2;

            Assert.False(
                AiAssessmentValidationService.IsValid(
                    CreateAssessment(),
                    rubric));
        }

        [Fact]
        public void MissingGuidance_ReturnsFalse()
        {
            var assessment = CreateAssessment();
            assessment.ImprovementGuidance = "";

            Assert.False(
                AiAssessmentValidationService.IsValid(
                    assessment,
                    CreateRubric()));
        }

        [Fact]
        public void AlternativeMarkAllocation_ReturnsTrue()
        {
            var rubric = CreateRubric();
            rubric.TechnicalAccuracyMarks = 5;
            rubric.KeyConceptMarks = 2;

            Assert.True(
                AiAssessmentValidationService.IsValid(
                    CreateAssessment(),
                    rubric));
        }
    }
}
