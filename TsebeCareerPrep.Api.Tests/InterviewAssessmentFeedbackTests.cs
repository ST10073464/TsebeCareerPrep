
using Microsoft.EntityFrameworkCore;
using TsebeCareerPrep.Api.Data;
using TsebeCareerPrep.Api.Models;
using TsebeCareerPrep.Api.Services;
using Xunit;

namespace TsebeCareerPrep.Api.Tests
{
    public class InterviewAssessmentFeedbackTests
    {
        private static ApplicationDbContext CreateContext()
        {
            var options =
                new DbContextOptionsBuilder<ApplicationDbContext>()
                    .UseInMemoryDatabase(
                        Guid.NewGuid().ToString())
                    .Options;

            return new ApplicationDbContext(options);
        }

        [Fact]
        public async Task GetFeedback_ValidAnswer_ReturnsGuidance()
        {
            using var context = CreateContext();

            context.InterviewResponses.Add(
                new InterviewResponse
                {
                    Id = 101,
                    InterviewQuestionId = 201,
                    InterviewSessionId = 301,
                    AnswerText =
                        "Classes and objects organise code."
                });

            context.InterviewQuestionRubrics.Add(
                new InterviewQuestionRubric
                {
                    InterviewQuestionId = 201,
                    ExpectedConcepts =
                        "Classes; objects; encapsulation",
                    AssessmentGuidance =
                        "Explain encapsulation and give an example.",
                    TechnicalAccuracyMarks = 4,
                    KeyConceptMarks = 3,
                    ClarityMarks = 2,
                    PracticalExampleMarks = 1,
                    IsActive = true
                });

            await context.SaveChangesAsync();

            var service =
                new InterviewAssessmentService(context);

            var feedback =
                await service.GetFeedbackAsync(101);

            Assert.NotNull(feedback);
            Assert.Equal(101, feedback.InterviewResponseId);
            Assert.Equal(201, feedback.InterviewQuestionId);

            Assert.Contains(
                "Classes",
                feedback.DetectedConcepts);

            Assert.Contains(
                "objects",
                feedback.DetectedConcepts);

            Assert.Contains(
                "encapsulation",
                feedback.ConceptsToReview);

            Assert.Equal(
                "Explain encapsulation and give an example.",
                feedback.ImprovementGuidance);

            Assert.True(feedback.RequiresManualReview);

            Assert.Equal(
                "Not formally scored",
                feedback.AssessmentStatus);

            Assert.Empty(context.InterviewAssessments);
        }

        [Fact]
        public async Task GetFeedback_MissingRubric_ReturnsNull()
        {
            using var context = CreateContext();

            context.InterviewResponses.Add(
                new InterviewResponse
                {
                    Id = 102,
                    InterviewQuestionId = 202,
                    InterviewSessionId = 302,
                    AnswerText = "A valid interview answer."
                });

            await context.SaveChangesAsync();

            var service =
                new InterviewAssessmentService(context);

            var feedback =
                await service.GetFeedbackAsync(102);

            Assert.Null(feedback);
            Assert.Empty(context.InterviewAssessments);
        }

        [Fact]
        public async Task GetFeedback_InvalidRubric_ReturnsNull()
        {
            using var context = CreateContext();

            context.InterviewResponses.Add(
                new InterviewResponse
                {
                    Id = 103,
                    InterviewQuestionId = 203,
                    InterviewSessionId = 303,
                    AnswerText =
                        "I use access controls and secure storage."
                });

            context.InterviewQuestionRubrics.Add(
                new InterviewQuestionRubric
                {
                    InterviewQuestionId = 203,
                    ExpectedConcepts =
                        "Access controls; secure storage",
                    TechnicalAccuracyMarks = 4,
                    KeyConceptMarks = 3,
                    ClarityMarks = 2,
                    PracticalExampleMarks = 2,
                    IsActive = true
                });

            await context.SaveChangesAsync();

            var service =
                new InterviewAssessmentService(context);

            var feedback =
                await service.GetFeedbackAsync(103);

            Assert.Null(feedback);
            Assert.Empty(context.InterviewAssessments);
        }

        [Fact]
        public async Task GetFeedback_UnknownResponse_ReturnsNull()
        {
            using var context = CreateContext();

            var service =
                new InterviewAssessmentService(context);

            var feedback =
                await service.GetFeedbackAsync(999);

            Assert.Null(feedback);
            Assert.Empty(context.InterviewAssessments);
        }
    }
}
