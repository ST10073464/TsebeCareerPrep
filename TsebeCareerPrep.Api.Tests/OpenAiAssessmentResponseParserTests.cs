
using System.Text.Json;
using TsebeCareerPrep.Api.Models;
using TsebeCareerPrep.Api.Services;
using Xunit;

namespace TsebeCareerPrep.Api.Tests
{
    public class OpenAiAssessmentResponseParserTests
    {
        private static InterviewQuestionRubric CreateRubric()
        {
            return new InterviewQuestionRubric
            {
                IsActive = true,
                ExpectedConcepts = "Classes; Objects",
                TechnicalAccuracyMarks = 4,
                KeyConceptMarks = 3,
                ClarityMarks = 2,
                PracticalExampleMarks = 1
            };
        }

        private static string CreateResponse(
            int suggestedScore = 8,
            int technicalScore = 3,
            bool requiresReview = true,
            string status = "completed")
        {
            var assessment = new
            {
                suggestedScore,
                technicalAccuracyScore = technicalScore,
                keyConceptScore = 2,
                clarityScore = 2,
                practicalExampleScore = 1,
                strengths = "Good explanation.",
                areasForImprovement = "Add more detail.",
                improvementGuidance =
                    "Explain the concepts with examples.",
                requiresManualReview = requiresReview
            };

            var response = new
            {
                status,
                output = new[]
                {
                    new
                    {
                        type = "message",
                        content = new[]
                        {
                            new
                            {
                                type = "output_text",
                                text = JsonSerializer.Serialize(
                                    assessment)
                            }
                        }
                    }
                }
            };

            return JsonSerializer.Serialize(response);
        }

        private static string CreateRawResponse(
            string assessmentJson)
        {
            var response = new
            {
                status = "completed",
                output = new[]
                {
                    new
                    {
                        type = "message",
                        content = new[]
                        {
                            new
                            {
                                type = "output_text",
                                text = assessmentJson
                            }
                        }
                    }
                }
            };

            return JsonSerializer.Serialize(response);
        }

        // Test 1: Accept a valid provisional assessment.
        [Fact]
        public void ValidResponse_ReturnsAssessment()
        {
            var result =
                OpenAiAssessmentResponseParser.Parse(
                    CreateResponse(),
                    CreateRubric());

            Assert.NotNull(result);
            Assert.Equal(8, result.SuggestedScore);
            Assert.True(result.RequiresManualReview);
        }

        // Test 2: Reject an incorrect total score.
        [Fact]
        public void IncorrectTotal_ReturnsNull()
        {
            var result =
                OpenAiAssessmentResponseParser.Parse(
                    CreateResponse(suggestedScore: 9),
                    CreateRubric());

            Assert.Null(result);
        }

        // Test 3: Reject marks exceeding the rubric.
        [Fact]
        public void ScoreExceedsRubric_ReturnsNull()
        {
            var result =
                OpenAiAssessmentResponseParser.Parse(
                    CreateResponse(
                        suggestedScore: 10,
                        technicalScore: 5),
                    CreateRubric());

            Assert.Null(result);
        }

        // Test 4: Human review must remain mandatory.
        [Fact]
        public void ManualReviewDisabled_ReturnsNull()
        {
            var result =
                OpenAiAssessmentResponseParser.Parse(
                    CreateResponse(requiresReview: false),
                    CreateRubric());

            Assert.Null(result);
        }

        // Test 5: Reject incomplete API responses.
        [Fact]
        public void IncompleteResponse_ReturnsNull()
        {
            var result =
                OpenAiAssessmentResponseParser.Parse(
                    CreateResponse(status: "incomplete"),
                    CreateRubric());

            Assert.Null(result);
        }

        // Test 6: Reject malformed JSON.
        [Fact]
        public void MalformedJson_ReturnsNull()
        {
            var result =
                OpenAiAssessmentResponseParser.Parse(
                    "{ invalid json",
                    CreateRubric());

            Assert.Null(result);
        }

        // Test 7: Reject an OpenAI refusal.
        [Fact]
        public void Refusal_ReturnsNull()
        {
            string response = """
                {
                  "status": "completed",
                  "output": [
                    {
                      "type": "message",
                      "content": [
                        {
                          "type": "refusal",
                          "refusal": "Cannot assess this answer."
                        }
                      ]
                    }
                  ]
                }
                """;

            var result =
                OpenAiAssessmentResponseParser.Parse(
                    response,
                    CreateRubric());

            Assert.Null(result);
        }

        // Test 8: Reject missing assessment fields.
        [Fact]
        public void MissingAssessmentField_ReturnsNull()
        {
            string assessmentJson = """
                {
                    "suggestedScore": 8,
                    "technicalAccuracyScore": 3,
                    "keyConceptScore": 2,
                    "clarityScore": 2,
                    "practicalExampleScore": 1,
                    "strengths": "Good explanation.",
                    "areasForImprovement": "Add more detail.",
                    "requiresManualReview": true
                }
                """;

            var result =
                OpenAiAssessmentResponseParser.Parse(
                    CreateRawResponse(assessmentJson),
                    CreateRubric());

            Assert.Null(result);
        }

        // Test 9: Reject unexpected assessment fields.
        [Fact]
        public void UnexpectedAssessmentField_ReturnsNull()
        {
            string assessmentJson = """
                {
                    "suggestedScore": 8,
                    "technicalAccuracyScore": 3,
                    "keyConceptScore": 2,
                    "clarityScore": 2,
                    "practicalExampleScore": 1,
                    "strengths": "Good explanation.",
                    "areasForImprovement": "Add more detail.",
                    "improvementGuidance": "Provide an example.",
                    "requiresManualReview": true,
                    "approved": true
                }
                """;

            var result =
                OpenAiAssessmentResponseParser.Parse(
                    CreateRawResponse(assessmentJson),
                    CreateRubric());

            Assert.Null(result);
        }

        // Test 10: Reject null assessment fields.
        [Fact]
        public void NullAssessmentField_ReturnsNull()
        {
            string assessmentJson = """
                {
                    "suggestedScore": 8,
                    "technicalAccuracyScore": 3,
                    "keyConceptScore": 2,
                    "clarityScore": 2,
                    "practicalExampleScore": 1,
                    "strengths": "Good explanation.",
                    "areasForImprovement": "Add more detail.",
                    "improvementGuidance": null,
                    "requiresManualReview": true
                }
                """;

            var result =
                OpenAiAssessmentResponseParser.Parse(
                    CreateRawResponse(assessmentJson),
                    CreateRubric());

            Assert.Null(result);
        }
    }
}
