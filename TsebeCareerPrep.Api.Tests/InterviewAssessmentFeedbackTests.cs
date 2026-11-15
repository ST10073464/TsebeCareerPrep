
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

        private static InterviewQuestionRubric CreateRubric(
            int questionId,
            string expectedConcepts,
            string alternatives = "")
        {
            return new InterviewQuestionRubric
            {
                InterviewQuestionId = questionId,
                ExpectedConcepts = expectedConcepts,
                AcceptableAlternatives = alternatives,
                AssessmentGuidance =
                    "Explain the concepts and provide an example.",
                TechnicalAccuracyMarks = 4,
                KeyConceptMarks = 3,
                ClarityMarks = 2,
                PracticalExampleMarks = 1,
                IsActive = true
            };
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

            var rubric = CreateRubric(
                201,
                "Classes; objects; encapsulation");

            rubric.AssessmentGuidance =
                "Assess whether the candidate explains encapsulation.";

            rubric.CandidateGuidance =
                "Explain encapsulation and give an example.";


            context.InterviewQuestionRubrics.Add(rubric);

            await context.SaveChangesAsync();

            var service =
                new InterviewAssessmentService(context);

            var feedback =
                await service.GetFeedbackAsync(101);

            Assert.NotNull(feedback);

            Assert.Equal(
                101,
                feedback.InterviewResponseId);

            Assert.Equal(
                201,
                feedback.InterviewQuestionId);

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

            Assert.Equal(
                67,
                feedback.ConceptCoveragePercentage);

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

        [Fact]
        public async Task GetFeedback_AllConceptsDetected_Returns100Percent()
        {
            using var context = CreateContext();

            context.InterviewResponses.Add(
                new InterviewResponse
                {
                    Id = 104,
                    InterviewQuestionId = 204,
                    InterviewSessionId = 304,
                    AnswerText =
                        "Classes contain objects and support encapsulation."
                });

            context.InterviewQuestionRubrics.Add(
                CreateRubric(
                    204,
                    "Classes; objects; encapsulation"));

            await context.SaveChangesAsync();

            var service =
                new InterviewAssessmentService(context);

            var feedback =
                await service.GetFeedbackAsync(104);

            Assert.NotNull(feedback);

            Assert.Equal(
                100,
                feedback.ConceptCoveragePercentage);

            Assert.Empty(feedback.ConceptsToReview);

            Assert.Empty(context.InterviewAssessments);
        }

        [Fact]
        public async Task GetFeedback_NoConceptsDetected_ReturnsZeroPercent()
        {
            using var context = CreateContext();

            context.InterviewResponses.Add(
                new InterviewResponse
                {
                    Id = 105,
                    InterviewQuestionId = 205,
                    InterviewSessionId = 305,
                    AnswerText =
                        "I would ask my supervisor for assistance."
                });

            context.InterviewQuestionRubrics.Add(
                CreateRubric(
                    205,
                    "Classes; objects; encapsulation"));

            await context.SaveChangesAsync();

            var service =
                new InterviewAssessmentService(context);

            var feedback =
                await service.GetFeedbackAsync(105);

            Assert.NotNull(feedback);

            Assert.Equal(
                0,
                feedback.ConceptCoveragePercentage);

            Assert.Equal(
                3,
                feedback.ConceptsToReview.Count);

            Assert.Empty(context.InterviewAssessments);
        }

        [Fact]
        public async Task GetFeedback_SubstringDoesNotCountAsConcept()
        {
            using var context = CreateContext();

            context.InterviewResponses.Add(
                new InterviewResponse
                {
                    Id = 106,
                    InterviewQuestionId = 206,
                    InterviewSessionId = 306,
                    AnswerText =
                        "The classification process is important."
                });

            context.InterviewQuestionRubrics.Add(
                CreateRubric(
                    206,
                    "class"));

            await context.SaveChangesAsync();

            var service =
                new InterviewAssessmentService(context);

            var feedback =
                await service.GetFeedbackAsync(106);

            Assert.NotNull(feedback);

            // "class" inside "classification" must not count.
            Assert.Equal(
                0,
                feedback.ConceptCoveragePercentage);

            Assert.Contains(
                "class",
                feedback.ConceptsToReview);
        }

        [Fact]
        public async Task GetFeedback_CaseInsensitivePhraseRecognition()
        {
            using var context = CreateContext();

            context.InterviewResponses.Add(
                new InterviewResponse
                {
                    Id = 107,
                    InterviewQuestionId = 207,
                    InterviewSessionId = 307,
                    AnswerText =
                        "ENCAPSULATION protects internal data."
                });

            context.InterviewQuestionRubrics.Add(
                CreateRubric(
                    207,
                    "encapsulation"));

            await context.SaveChangesAsync();

            var service =
                new InterviewAssessmentService(context);

            var feedback =
                await service.GetFeedbackAsync(107);

            Assert.NotNull(feedback);

            Assert.Equal(
                100,
                feedback.ConceptCoveragePercentage);
        }

        [Fact]
        public async Task GetFeedback_UnmappedAlternativeDoesNotAwardCredit()
        {
            using var context = CreateContext();

            context.InterviewResponses.Add(
                new InterviewResponse
                {
                    Id = 108,
                    InterviewQuestionId = 208,
                    InterviewSessionId = 308,
                    AnswerText =
                        "OOP is useful for organising applications."
                });

            context.InterviewQuestionRubrics.Add(
                CreateRubric(
                    208,
                    "Classes; objects; encapsulation",
                    "OOP; object-oriented design"));

            await context.SaveChangesAsync();

            var service =
                new InterviewAssessmentService(context);

            var feedback =
                await service.GetFeedbackAsync(108);

            Assert.NotNull(feedback);

            // Alternatives must be explicitly associated
            // with an expected concept before awarding credit.
            Assert.Equal(
                0,
                feedback.ConceptCoveragePercentage);
        }


        [Fact]
        public async Task GetFeedback_MappedAlternative_CountsExpectedConcept()
        {
            using var context = CreateContext();

            context.InterviewResponses.Add(
                new InterviewResponse
                {
                    Id = 109,
                    InterviewQuestionId = 209,
                    InterviewSessionId = 309,
                    AnswerText =
                        "A class is a blueprint used to create instances. " +
                        "Data hiding protects internal information."
                });

            context.InterviewQuestionRubrics.Add(
                CreateRubric(
                    209,
                    "Classes; Objects; Encapsulation",
                    "Classes = class | class blueprint\n" +
                    "Objects = object | instances\n" +
                    "Encapsulation = data hiding | information hiding"));

            await context.SaveChangesAsync();

            var service =
                new InterviewAssessmentService(context);

            var feedback =
                await service.GetFeedbackAsync(109);

            Assert.NotNull(feedback);

            Assert.Equal(
                100,
                feedback.ConceptCoveragePercentage);

            Assert.Equal(
                3,
                feedback.DetectedConcepts.Count);

            Assert.Empty(feedback.ConceptsToReview);

            Assert.Empty(context.InterviewAssessments);
        }


        [Fact]
        public async Task GetFeedback_MissingCandidateGuidance_UsesSafeFallback()
        {
            using var context = CreateContext();

            context.InterviewResponses.Add(
                new InterviewResponse
                {
                    Id = 110,
                    InterviewQuestionId = 210,
                    InterviewSessionId = 310,
                    AnswerText = "Classes and objects organise code."
                });

            var rubric = CreateRubric(
                210,
                "Classes; objects; encapsulation");

            rubric.AssessmentGuidance =
                "Assessor only: award marks for correct explanations.";

            rubric.CandidateGuidance = string.Empty;

            context.InterviewQuestionRubrics.Add(rubric);

            await context.SaveChangesAsync();

            var service = new InterviewAssessmentService(context);

            var feedback = await service.GetFeedbackAsync(110);

            Assert.NotNull(feedback);

            Assert.Contains(
                "Review the suggested answer",
                feedback.ImprovementGuidance);

            Assert.DoesNotContain(
                "Assessor only",
                feedback.ImprovementGuidance);

            Assert.True(feedback.RequiresManualReview);

            Assert.Equal(
                "Not formally scored",
                feedback.AssessmentStatus);

            Assert.Empty(context.InterviewAssessments);
        }


        [Fact]
        public async Task GetFeedback_IncorrectStatement_ReturnsMisconceptionWarning()
        {
            using var context = CreateContext();

            context.InterviewResponses.Add(
                new InterviewResponse
                {
                    Id = 111,
                    InterviewQuestionId = 211,
                    InterviewSessionId = 311,
                    AnswerText =
                        "Object-oriented programming means " +
                        "classes are database tables."
                });

            var rubric = CreateRubric(
                211,
                "Classes; objects; encapsulation; reusable code");

            rubric.CandidateGuidance =
                "Explain how classes and objects work together.";

            context.InterviewQuestionRubrics.Add(rubric);

            context.InterviewMisconceptionRules.Add(
                new InterviewMisconceptionRule
                {
                    InterviewQuestionId = 211,
                    TriggerPhrase = "classes are database tables",
                    FeedbackMessage =
                        "Classes define structures and behaviours " +
                        "for objects. They are not inherently database tables.",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                });

            await context.SaveChangesAsync();

            var service = new InterviewAssessmentService(context);

            var feedback = await service.GetFeedbackAsync(111);

            Assert.NotNull(feedback);

            Assert.Single(feedback.PotentialMisconceptions);

            Assert.Contains(
                "Classes define structures and behaviours",
                feedback.PotentialMisconceptions[0]);

            Assert.Contains(
                "Potential misconceptions were detected",
                feedback.ReviewReason);

            Assert.Equal(
                "Explain how classes and objects work together.",
                feedback.ImprovementGuidance);

            Assert.True(feedback.RequiresManualReview);

            Assert.Equal(
                "Not formally scored",
                feedback.AssessmentStatus);

            Assert.Empty(context.InterviewAssessments);
        }


        [Fact]
        public async Task GetFeedback_ExplicitlyRejectedMisconception_DoesNotWarn()
        {
            using var context = CreateContext();

            context.InterviewResponses.Add(
                new InterviewResponse
                {
                    Id = 112,
                    InterviewQuestionId = 212,
                    InterviewSessionId = 312,
                    AnswerText =
                        "Object-oriented programming uses classes and objects. " +
                        "It is false that classes are database tables. " +
                        "Classes define structures and behaviours for objects."
                });

            var rubric = CreateRubric(
                212,
                "Classes; objects; encapsulation; reusable code");

            rubric.CandidateGuidance =
                "Explain how classes and objects work together.";

            context.InterviewQuestionRubrics.Add(rubric);

            context.InterviewMisconceptionRules.Add(
                new InterviewMisconceptionRule
                {
                    InterviewQuestionId = 212,
                    TriggerPhrase = "classes are database tables",
                    FeedbackMessage =
                        "Classes define structures and behaviours " +
                        "for objects. They are not inherently database tables.",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                });

            await context.SaveChangesAsync();

            var service = new InterviewAssessmentService(context);

            var feedback = await service.GetFeedbackAsync(112);

            Assert.NotNull(feedback);

            Assert.Empty(feedback.PotentialMisconceptions);

            Assert.DoesNotContain(
                "Potential misconceptions were detected",
                feedback.ReviewReason);

            Assert.True(feedback.RequiresManualReview);

            Assert.Equal(
                "Not formally scored",
                feedback.AssessmentStatus);

            Assert.Empty(context.InterviewAssessments);
        }


        [Fact]
        public async Task GetFeedback_RejectedThenAssertedMisconception_ReturnsWarning()
        {
            using var context = CreateContext();

            context.InterviewResponses.Add(
                new InterviewResponse
                {
                    Id = 113,
                    InterviewQuestionId = 213,
                    InterviewSessionId = 313,
                    AnswerText =
                        "It is false that classes are database tables. " +
                        "However, classes are database tables."
                });

            var rubric = CreateRubric(
                213,
                "Classes; objects; encapsulation; reusable code");

            rubric.CandidateGuidance =
                "Explain how classes and objects work together.";

            context.InterviewQuestionRubrics.Add(rubric);

            context.InterviewMisconceptionRules.Add(
                new InterviewMisconceptionRule
                {
                    InterviewQuestionId = 213,
                    TriggerPhrase = "classes are database tables",
                    FeedbackMessage =
                        "Classes define structures and behaviours " +
                        "for objects. They are not inherently database tables.",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                });

            await context.SaveChangesAsync();

            var service = new InterviewAssessmentService(context);

            var feedback = await service.GetFeedbackAsync(113);

            Assert.NotNull(feedback);

            Assert.Single(feedback.PotentialMisconceptions);

            Assert.Contains(
                "Classes define structures and behaviours",
                feedback.PotentialMisconceptions[0]);

            Assert.Contains(
                "Potential misconceptions were detected",
                feedback.ReviewReason);

            Assert.True(feedback.RequiresManualReview);

            Assert.Equal(
                "Not formally scored",
                feedback.AssessmentStatus);

            Assert.Empty(context.InterviewAssessments);
        }


        [Fact]
        public async Task GetFeedback_InactiveMisconceptionRule_DoesNotWarn()
        {
            using var context = CreateContext();

            context.InterviewResponses.Add(
                new InterviewResponse
                {
                    Id = 114,
                    InterviewQuestionId = 214,
                    InterviewSessionId = 314,
                    AnswerText =
                        "Object-oriented programming means " +
                        "classes are database tables."
                });

            var rubric = CreateRubric(
                214,
                "Classes; objects; encapsulation; reusable code");

            rubric.CandidateGuidance =
                "Explain how classes and objects work together.";

            context.InterviewQuestionRubrics.Add(rubric);

            context.InterviewMisconceptionRules.Add(
                new InterviewMisconceptionRule
                {
                    InterviewQuestionId = 214,
                    TriggerPhrase = "classes are database tables",
                    FeedbackMessage =
                        "Classes define structures and behaviours for objects.",
                    IsActive = false,
                    CreatedAt = DateTime.UtcNow
                });

            await context.SaveChangesAsync();

            var service = new InterviewAssessmentService(context);

            var feedback = await service.GetFeedbackAsync(114);

            Assert.NotNull(feedback);

            Assert.Empty(feedback.PotentialMisconceptions);

            Assert.DoesNotContain(
                "Potential misconceptions were detected",
                feedback.ReviewReason);

            Assert.True(feedback.RequiresManualReview);

            Assert.Equal(
                "Not formally scored",
                feedback.AssessmentStatus);

            Assert.Empty(context.InterviewAssessments);
        }




    }
}
