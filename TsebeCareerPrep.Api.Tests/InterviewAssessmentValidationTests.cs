
using Microsoft.EntityFrameworkCore;
using TsebeCareerPrep.Api.Data;
using TsebeCareerPrep.Api.Models;
using TsebeCareerPrep.Api.Services;
using Xunit;

namespace TsebeCareerPrep.Api.Tests
{
    public class InterviewAssessmentValidationTests
    {
        private static ApplicationDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            return new ApplicationDbContext(options);
        }

        [Fact]
        public async Task AssessResponse_EmptyAnswer_ReturnsNull()
        {
            using var context = CreateContext();

            context.InterviewResponses.Add(new InterviewResponse
            {
                Id = 1,
                InterviewQuestionId = 1,
                AnswerText = "   "
            });

            context.InterviewQuestionRubrics.Add(
                new InterviewQuestionRubric
                {
                    InterviewQuestionId = 1,
                    ExpectedConcepts = "Classes; objects"
                });

            await context.SaveChangesAsync();

            var service = new InterviewAssessmentService(context);

            var result = await service.AssessResponseAsync(1);

            Assert.Null(result);
            Assert.Empty(context.InterviewAssessments);
        }

        [Fact]
        public async Task AssessResponse_MissingRubric_ReturnsNull()
        {
            using var context = CreateContext();

            context.InterviewResponses.Add(new InterviewResponse
            {
                Id = 2,
                InterviewQuestionId = 2,
                AnswerText = "A valid sample interview answer."
            });

            await context.SaveChangesAsync();

            var service = new InterviewAssessmentService(context);

            var result = await service.AssessResponseAsync(2);

            Assert.Null(result);
            Assert.Empty(context.InterviewAssessments);
        }

        [Fact]
        public async Task AssessResponse_InvalidMarkTotal_ReturnsNull()
        {
            using var context = CreateContext();

            context.InterviewResponses.Add(new InterviewResponse
            {
                Id = 3,
                InterviewQuestionId = 3,
                AnswerText = "I prioritise deadlines and importance."
            });

            context.InterviewQuestionRubrics.Add(
                new InterviewQuestionRubric
                {
                    InterviewQuestionId = 3,
                    ExpectedConcepts = "Deadlines; importance",
                    TechnicalAccuracyMarks = 4,
                    KeyConceptMarks = 3,
                    ClarityMarks = 2,
                    PracticalExampleMarks = 2
                });

            await context.SaveChangesAsync();

            var service = new InterviewAssessmentService(context);

            var result = await service.AssessResponseAsync(3);

            Assert.Null(result);
            Assert.Empty(context.InterviewAssessments);
        }

        [Fact]
        public async Task AssessResponse_ValidAnswer_RequiresReviewWithoutScoring()
        {
            // Arrange: create an isolated test database.
            using var context = CreateContext();

            // Create a valid candidate response.
            context.InterviewResponses.Add(new InterviewResponse
            {
                Id = 4,
                InterviewQuestionId = 4,
                AnswerText =
                    "I protect confidential documents using " +
                    "access controls, organisational policies " +
                    "and secure storage procedures."
            });

            // Create a valid 10-mark assessment rubric.
            context.InterviewQuestionRubrics.Add(
                new InterviewQuestionRubric
                {
                    InterviewQuestionId = 4,
                    ExpectedConcepts =
                        "Access controls; organisational policies; " +
                        "secure storage; confidentiality",
                    AcceptableAlternatives =
                        "Restricted access; information security; privacy",
                    TechnicalAccuracyMarks = 4,
                    KeyConceptMarks = 3,
                    ClarityMarks = 2,
                    PracticalExampleMarks = 1,
                    IsActive = true
                });

            await context.SaveChangesAsync();

            var service = new InterviewAssessmentService(context);

            // Act: attempt to assess the valid response.
            var result = await service.AssessResponseAsync(4);

            // Assert: automatic scoring is not yet enabled.
            Assert.Null(result);

            // No unverified assessment must be saved.
            Assert.Empty(context.InterviewAssessments);

            // The submitted answer must remain unchanged.
            var savedResponse =
                await context.InterviewResponses
                    .SingleAsync(r => r.Id == 4);

            Assert.Contains(
                "access controls",
                savedResponse.AnswerText);
        }
    }
}
