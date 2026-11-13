
using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using TsebeCareerPrep.Api.Models;
using TsebeCareerPrep.Api.Services;
using Xunit;

namespace TsebeCareerPrep.Api.Tests
{
    public class GeminiInterviewAssessmentServiceTests
    {
        [Fact]
        public async Task ValidGeminiResponse_ReturnsProvisionalAssessment()
        {
            var handler = new FakeHttpMessageHandler(
                HttpStatusCode.OK,
                CreateValidGeminiResponse());

            using var client = CreateClient(handler);
            var service = CreateService(client);

            var result =
                await service.GenerateProvisionalAssessmentAsync(
                    CreateInterviewResponse(),
                    CreateRubric());

            Assert.NotNull(result);
            Assert.Equal(8, result.SuggestedScore);
            Assert.Equal(3, result.TechnicalAccuracyScore);
            Assert.Equal(2, result.KeyConceptScore);
            Assert.Equal(2, result.ClarityScore);
            Assert.Equal(1, result.PracticalExampleScore);
            Assert.True(result.RequiresManualReview);
        }

        [Theory]
        [InlineData(HttpStatusCode.Unauthorized)]
        [InlineData(HttpStatusCode.TooManyRequests)]
        [InlineData(HttpStatusCode.InternalServerError)]
        public async Task ApiError_ReturnsNull(
            HttpStatusCode statusCode)
        {
            var handler = new FakeHttpMessageHandler(
                statusCode,
                """{"error":{"message":"Simulated failure"}}""");

            using var client = CreateClient(handler);
            var service = CreateService(client);

            var result =
                await service.GenerateProvisionalAssessmentAsync(
                    CreateInterviewResponse(),
                    CreateRubric());

            Assert.Null(result);
        }

        [Fact]
        public async Task MalformedGeminiResponse_ReturnsNull()
        {
            var handler = new FakeHttpMessageHandler(
                HttpStatusCode.OK,
                "{ invalid json");

            using var client = CreateClient(handler);
            var service = CreateService(client);

            var result =
                await service.GenerateProvisionalAssessmentAsync(
                    CreateInterviewResponse(),
                    CreateRubric());

            Assert.Null(result);
        }

        [Fact]
        public async Task MissingCandidates_ReturnsNull()
        {
            var handler = new FakeHttpMessageHandler(
                HttpStatusCode.OK,
                """{"candidates":[]}""");

            using var client = CreateClient(handler);
            var service = CreateService(client);

            var result =
                await service.GenerateProvisionalAssessmentAsync(
                    CreateInterviewResponse(),
                    CreateRubric());

            Assert.Null(result);
        }

        [Fact]
        public async Task Timeout_ReturnsNull()
        {
            using var client = new HttpClient(
                new TimeoutHttpMessageHandler())
            {
                BaseAddress = new Uri(
                    "https://generativelanguage.googleapis.com/v1beta/")
            };

            var service = CreateService(client);

            var result =
                await service.GenerateProvisionalAssessmentAsync(
                    CreateInterviewResponse(),
                    CreateRubric());

            Assert.Null(result);
        }

        [Fact]
        public async Task InvalidAssessmentScore_ReturnsNull()
        {
            var assessment = new
            {
                suggestedScore = 15,
                technicalAccuracyScore = 9,
                keyConceptScore = 3,
                clarityScore = 2,
                practicalExampleScore = 1,
                strengths = "Good explanation.",
                areasForImprovement = "More detail.",
                improvementGuidance = "Add an example.",
                requiresManualReview = true
            };

            var handler = new FakeHttpMessageHandler(
                HttpStatusCode.OK,
                WrapGeminiResponse(
                    JsonSerializer.Serialize(assessment)));

            using var client = CreateClient(handler);
            var service = CreateService(client);

            var result =
                await service.GenerateProvisionalAssessmentAsync(
                    CreateInterviewResponse(),
                    CreateRubric());

            Assert.Null(result);
        }

        [Fact]
        public async Task ManualReviewFalse_ReturnsNull()
        {
            var assessment = new
            {
                suggestedScore = 8,
                technicalAccuracyScore = 3,
                keyConceptScore = 2,
                clarityScore = 2,
                practicalExampleScore = 1,
                strengths = "Good explanation.",
                areasForImprovement = "More detail.",
                improvementGuidance = "Add an example.",
                requiresManualReview = false
            };

            var handler = new FakeHttpMessageHandler(
                HttpStatusCode.OK,
                WrapGeminiResponse(
                    JsonSerializer.Serialize(assessment)));

            using var client = CreateClient(handler);
            var service = CreateService(client);

            var result =
                await service.GenerateProvisionalAssessmentAsync(
                    CreateInterviewResponse(),
                    CreateRubric());

            Assert.Null(result);
        }

        [Fact]
        public async Task OutgoingRequest_UsesGeminiEndpointAndApiKey()
        {
            var handler = new FakeHttpMessageHandler(
                HttpStatusCode.BadRequest,
                """{"error":{"message":"Simulated response"}}""");

            using var client = CreateClient(handler);
            var service = CreateService(client);

            var result =
                await service.GenerateProvisionalAssessmentAsync(
                    CreateInterviewResponse(),
                    CreateRubric());

            Assert.Null(result);

            Assert.Equal(
                HttpMethod.Post,
                handler.RequestMethod);

            Assert.Equal(
                "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent",
                handler.RequestUri);

            Assert.Equal("test-gemini-key", handler.ApiKey);
            Assert.Equal("application/json", handler.ContentType);

            Assert.NotNull(handler.RequestBody);

            using var document =
                JsonDocument.Parse(handler.RequestBody!);

            var root = document.RootElement;

            var systemText = root
                .GetProperty("systemInstruction")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString()!;

            Assert.Contains(
                "provisional",
                systemText,
                StringComparison.OrdinalIgnoreCase);

            var inputText = root
                .GetProperty("contents")[0]
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString()!;

            using var inputDocument =
                JsonDocument.Parse(inputText);

            Assert.Equal(
                "Explain classes and objects.",
                inputDocument.RootElement
                    .GetProperty("InterviewQuestion")
                    .GetString());

            Assert.Equal(
                "A class defines objects and their behaviour.",
                inputDocument.RootElement
                    .GetProperty("CandidateAnswer")
                    .GetString());

            var generationConfig =
                root.GetProperty("generationConfig");

            Assert.Equal(
                "application/json",
                generationConfig
                    .GetProperty("responseMimeType")
                    .GetString());

            Assert.Equal(
                1200,
                generationConfig
                    .GetProperty("maxOutputTokens")
                    .GetInt32());
        }

        private static string CreateValidGeminiResponse()
        {
            var assessment = new
            {
                suggestedScore = 8,
                technicalAccuracyScore = 3,
                keyConceptScore = 2,
                clarityScore = 2,
                practicalExampleScore = 1,
                strengths = "Good understanding.",
                areasForImprovement = "More detail needed.",
                improvementGuidance = "Provide a practical example.",
                requiresManualReview = true
            };

            return WrapGeminiResponse(
                JsonSerializer.Serialize(assessment));
        }

        private static string WrapGeminiResponse(string assessmentJson)
        {
            return JsonSerializer.Serialize(new
            {
                candidates = new[]
                {
                    new
                    {
                        content = new
                        {
                            parts = new[]
                            {
                                new { text = assessmentJson }
                            }
                        }
                    }
                }
            });
        }

        private static HttpClient CreateClient(
            HttpMessageHandler handler)
        {
            return new HttpClient(handler)
            {
                BaseAddress = new Uri(
                    "https://generativelanguage.googleapis.com/v1beta/")
            };
        }

        private static GeminiInterviewAssessmentService
            CreateService(HttpClient client)
        {
            return new GeminiInterviewAssessmentService(
                new FakeHttpClientFactory(client),
                CreateConfiguration());
        }

        private static IConfiguration CreateConfiguration()
        {
            return new ConfigurationBuilder()
                .AddInMemoryCollection(
                    new Dictionary<string, string?>
                    {
                        ["Gemini:ApiKey"] = "test-gemini-key",
                        ["Gemini:Model"] = "gemini-2.5-flash"
                    })
                .Build();
        }

        private static InterviewResponse CreateInterviewResponse()
        {
            return new InterviewResponse
            {
                AnswerText =
                    "A class defines objects and their behaviour.",
                InterviewQuestion = new InterviewQuestion
                {
                    QuestionText = "Explain classes and objects."
                }
            };
        }

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

        private sealed class FakeHttpMessageHandler
            : HttpMessageHandler
        {
            private readonly HttpStatusCode _statusCode;
            private readonly string _responseJson;

            public HttpMethod? RequestMethod { get; private set; }
            public string? RequestUri { get; private set; }
            public string? ApiKey { get; private set; }
            public string? ContentType { get; private set; }
            public string? RequestBody { get; private set; }

            public FakeHttpMessageHandler(
                HttpStatusCode statusCode,
                string responseJson)
            {
                _statusCode = statusCode;
                _responseJson = responseJson;
            }

            protected override async Task<HttpResponseMessage>
                SendAsync(
                    HttpRequestMessage request,
                    CancellationToken cancellationToken)
            {
                RequestMethod = request.Method;
                RequestUri = request.RequestUri?.ToString();

                if (request.Headers.TryGetValues(
                    "x-goog-api-key", out var values))
                {
                    ApiKey = values.FirstOrDefault();
                }

                ContentType =
                    request.Content?.Headers.ContentType?.MediaType;

                RequestBody = request.Content == null
                    ? null
                    : await request.Content.ReadAsStringAsync(
                        cancellationToken);

                return new HttpResponseMessage(_statusCode)
                {
                    Content = new StringContent(
                        _responseJson,
                        Encoding.UTF8,
                        "application/json")
                };
            }
        }

        private sealed class TimeoutHttpMessageHandler
            : HttpMessageHandler
        {
            protected override Task<HttpResponseMessage>
                SendAsync(
                    HttpRequestMessage request,
                    CancellationToken cancellationToken)
            {
                throw new TaskCanceledException(
                    "Simulated HTTP timeout.");
            }
        }

        private sealed class FakeHttpClientFactory
            : IHttpClientFactory
        {
            private readonly HttpClient _client;

            public FakeHttpClientFactory(HttpClient client)
            {
                _client = client;
            }

            public HttpClient CreateClient(string name)
            {
                Assert.Equal("Gemini", name);
                return _client;
            }
        }
    }
}
