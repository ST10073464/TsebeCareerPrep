
using TsebeCareerPrep.Api.Models;
using TsebeCareerPrep.Api.Services;
using Xunit;

namespace TsebeCareerPrep.Api.Tests
{
    public class InterviewAssessmentAlternativesTests
    {
        private static InterviewAssessmentEvidence Detect(
            string answer,
            string expected,
            string alternatives = "")
        {
            var response = new InterviewResponse
            {
                Id = 100,
                InterviewQuestionId = 1,
                AnswerText = answer
            };

            var rubric = new InterviewQuestionRubric
            {
                InterviewQuestionId = 1,
                ExpectedConcepts = expected,
                AcceptableAlternatives = alternatives,
                IsActive = true
            };

            return InterviewAssessmentService.DetectEvidence(
                response, rubric);
        }

        [Fact]
        public void ApprovedAlternative_IsDetected()
        {
            var result = Detect(
                "This encourages code reuse.",
                "reusable code",
                "reusable code = code reuse");

            Assert.Contains(
                "reusable code",
                result.DetectedConcepts);

            Assert.Empty(result.MissingConcepts);
        }

        [Fact]
        public void AlternativeMatching_IsCaseInsensitive()
        {
            var result = Detect(
                "CODE REUSE is important.",
                "reusable code",
                "reusable code = code reuse");

            Assert.Single(result.DetectedConcepts);
        }

        [Fact]
        public void MultipleAlternatives_AreSupported()
        {
            var result = Detect(
                "An instance represents an object.",
                "objects",
                "objects = object instances | instance");

            Assert.Contains(
                "objects",
                result.DetectedConcepts);
        }

        [Fact]
        public void UnapprovedAlternative_IsNotDetected()
        {
            var result = Detect(
                "This supports code reuse.",
                "reusable code");

            Assert.Empty(result.DetectedConcepts);

            Assert.Contains(
                "reusable code",
                result.MissingConcepts);
        }

        [Fact]
        public void PartialWords_DoNotMatch()
        {
            var result = Detect(
                "The system uses subclasses.",
                "classes");

            Assert.Empty(result.DetectedConcepts);
        }

        [Fact]
        public void UnmappedAlternative_IsIgnored()
        {
            var result = Detect(
                "Code reuse is useful.",
                "reusable code",
                "code reuse");

            Assert.Empty(result.DetectedConcepts);
        }

        [Fact]
        public void CompleteCoverage_StillRequiresManualReview()
        {
            var result = Detect(
                "Classes and objects support code reuse.",
                "classes; objects; reusable code",
                "reusable code = code reuse");

            Assert.Equal(3, result.DetectedConcepts.Count);
            Assert.Empty(result.MissingConcepts);
            Assert.True(result.RequiresManualReview);
        }

        [Fact]
        public void UnknownConceptMapping_IsIgnored()
        {
            var result = Detect(
                "Dependency injection is useful.",
                "encapsulation",
                "unknown concept = dependency injection");

            Assert.Empty(result.DetectedConcepts);
        }
    }
}
