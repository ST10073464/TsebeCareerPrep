
using TsebeCareerPrep.Api.Models;
using TsebeCareerPrep.Api.Services;
using Xunit;

namespace TsebeCareerPrep.Api.Tests
{
    public class InterviewAssessmentEvidenceTests
    {
        [Fact]
        public void DetectEvidence_IdentifiesPresentAndMissingConcepts()
        {
            // Arrange
            var response = new InterviewResponse
            {
                Id = 1,
                InterviewQuestionId = 1,
                AnswerText =
                    "Object-oriented programming uses " +
                    "classes and objects to organise code."
            };

            var rubric = new InterviewQuestionRubric
            {
                InterviewQuestionId = 1,
                ExpectedConcepts =
                    "Classes; objects; encapsulation; reusable code",
                TechnicalAccuracyMarks = 4,
                KeyConceptMarks = 3,
                ClarityMarks = 2,
                PracticalExampleMarks = 1,
                IsActive = true
            };

            // Act
            var evidence =
                InterviewAssessmentService.DetectEvidence(
                    response, rubric);

            // Assert
            Assert.Contains(
                "Classes", evidence.DetectedConcepts);

            Assert.Contains(
                "objects", evidence.DetectedConcepts);

            Assert.Contains(
                "encapsulation", evidence.MissingConcepts);

            Assert.Contains(
                "reusable code", evidence.MissingConcepts);

            Assert.Equal(1, evidence.InterviewResponseId);
            Assert.Equal(1, evidence.InterviewQuestionId);

            Assert.True(evidence.RequiresManualReview);
        }

        [Fact]
        public void DetectEvidence_AllConceptsPresent_StillRequiresManualReview()
        {
            // Arrange
            var response = new InterviewResponse
            {
                Id = 2,
                InterviewQuestionId = 1,
                AnswerText =
                    "Classes and objects support encapsulation " +
                    "and reusable code."
            };

            var rubric = new InterviewQuestionRubric
            {
                InterviewQuestionId = 1,
                ExpectedConcepts =
                    "Classes; objects; encapsulation; reusable code"
            };

            // Act
            var evidence =
                InterviewAssessmentService.DetectEvidence(
                    response, rubric);

            // Assert
            Assert.Equal(4, evidence.DetectedConcepts.Count);
            Assert.Empty(evidence.MissingConcepts);

            // Even when all concepts are detected,
            // the answer must still be reviewed.
            Assert.True(evidence.RequiresManualReview);

            Assert.False(
                string.IsNullOrWhiteSpace(
                    evidence.ReviewReason));
        }
    }
}
