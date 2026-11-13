
using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using TsebeCareerPrep.Api.Models;
using TsebeCareerPrep.Api.Services;
using Xunit;

namespace TsebeCareerPrep.Api.Tests
{
    public class AiInterviewAssessmentServiceTests
    {
        [Fact]
        public async Task ValidOpenAiResponse_ReturnsProvisionalAssessment()
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

            var apiResponse = new
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
                                text = JsonSerializer.Serialize(
                                    assessment)
                            }
                        }
                    }
                }
            };

            string responseJson =
                JsonSerializer.Serialize(apiResponse);

            var handler = new FakeHttpMessageHandler(
                HttpStatusCode.OK,
                responseJson);

            using var client = new HttpClient(handler)
            {
                BaseAddress = new Uri(
                    "https://api.openai.com/v1/")
            };

            var service = new AiInterviewAssessmentService(
                new FakeHttpClientFactory(client),
                CreateConfiguration());

            var result =
                await service.GenerateProvisionalAssessmentAsync(
                    CreateInterviewResponse(),
                    CreateRubric());

            Assert.NotNull(result);
            Assert.Equal(8, result.SuggestedScore);
            Assert.True(result.RequiresManualReview);

            Assert.Equal(
                HttpMethod.Post,
                handler.RequestMethod);

            Assert.Equal(
                "https://api.openai.com/v1/responses",
                handler.RequestUri);
        }

        [Theory]
        [InlineData(HttpStatusCode.Unauthorized)]
        [InlineData(HttpStatusCode.TooManyRequests)]
        [InlineData(HttpStatusCode.InternalServerError)]
        public async Task UnsuccessfulApiResponse_ReturnsNull(
            HttpStatusCode statusCode)
        {
            var handler = new FakeHttpMessageHandler(
                statusCode,
                """
                {
                    "error": {
                        "message": "Simulated API failure."
                    }
                }
                """);

            using var client = new HttpClient(handler)
            {
                BaseAddress = new Uri(
                    "https://api.openai.com/v1/")
            };

            var service = new AiInterviewAssessmentService(
                new FakeHttpClientFactory(client),
                CreateConfiguration());

            var result =
                await service.GenerateProvisionalAssessmentAsync(
                    CreateInterviewResponse(),
                    CreateRubric());

            Assert.Null(result);

            Assert.Equal(
                HttpMethod.Post,
                handler.RequestMethod);

            Assert.Equal(
                "https://api.openai.com/v1/responses",
                handler.RequestUri);
        }

        [Fact]
        public async Task MalformedApiResponse_ReturnsNull()
        {
            var handler = new FakeHttpMessageHandler(
                HttpStatusCode.OK,
                "{ invalid json");

            using var client = new HttpClient(handler)
            {
                BaseAddress = new Uri(
                    "https://api.openai.com/v1/")
            };

            var service = new AiInterviewAssessmentService(
                new FakeHttpClientFactory(client),
                CreateConfiguration());

            var result =
                await service.GenerateProvisionalAssessmentAsync(
                    CreateInterviewResponse(),
                    CreateRubric());

            Assert.Null(result);
            Assert.Equal(
                HttpMethod.Post,
                handler.RequestMethod);
        }

        [Fact]
        public async Task HttpTimeout_ReturnsNull()
        {
            using var client = new HttpClient(
                new TimeoutHttpMessageHandler())
            {
                BaseAddress = new Uri(
                    "https://api.openai.com/v1/")
            };

            var service = new AiInterviewAssessmentService(
                new FakeHttpClientFactory(client),
                CreateConfiguration());

            var result =
                await service.GenerateProvisionalAssessmentAsync(
                    CreateInterviewResponse(),
                    CreateRubric());

            Assert.Null(result);
        }

        // New test: verify the complete outgoing request.
        [Fact]
        public async Task OutgoingRequest_ContainsExpectedPayloadAndAuthorization()
        {
            var handler = new FakeHttpMessageHandler(
                HttpStatusCode.BadRequest,
                """
                {
                    "error": {
                        "message": "Simulated response."
                    }
                }
                """);

            using var client = new HttpClient(handler)
            {
                BaseAddress = new Uri(
                    "https://api.openai.com/v1/")
            };

            var service = new AiInterviewAssessmentService(
                new FakeHttpClientFactory(client),
                CreateConfiguration());

            var result =
                await service.GenerateProvisionalAssessmentAsync(
                    CreateInterviewResponse(),
                    CreateRubric());

            // A simulated HTTP error must not produce a score.
            Assert.Null(result);

            Assert.Equal(
                HttpMethod.Post,
                handler.RequestMethod);

            Assert.Equal(
                "https://api.openai.com/v1/responses",
                handler.RequestUri);

            Assert.Equal(
                "Bearer",
                handler.AuthorizationScheme);

            Assert.Equal(
                "test-key",
                handler.AuthorizationParameter);

            Assert.Equal(
                "application/json",
                handler.ContentType);

            Assert.NotNull(handler.RequestBody);

            using var document =
                JsonDocument.Parse(handler.RequestBody!);

            var root = document.RootElement;

            Assert.Equal(
                "gpt-4.1-mini",
                root.GetProperty("model").GetString());

            string instructions =
                root.GetProperty("instructions").GetString()!;

            Assert.Contains(
                "provisional",
                instructions,
                StringComparison.OrdinalIgnoreCase);

            Assert.Contains(
                "human reviewer",
                instructions,
                StringComparison.OrdinalIgnoreCase);

            string inputJson =
                root.GetProperty("input").GetString()!;

            using var inputDocument =
                JsonDocument.Parse(inputJson);

            var input = inputDocument.RootElement;

            Assert.Equal(
                "Explain classes and objects.",
                input.GetProperty(
                    "InterviewQuestion").GetString());

            Assert.Equal(
                "A class defines objects and their behaviour.",
                input.GetProperty(
                    "CandidateAnswer").GetString());

            Assert.Equal(
                "Classes; Objects",
                input.GetProperty(
                    "ExpectedConcepts").GetString());

            var marks = input.GetProperty("MaximumMarks");

            Assert.Equal(
                4,
                marks.GetProperty(
                    "TechnicalAccuracy").GetInt32());

            Assert.Equal(
                3,
                marks.GetProperty(
                    "KeyConcepts").GetInt32());

            Assert.Equal(
                2,
                marks.GetProperty(
                    "Clarity").GetInt32());

            Assert.Equal(
                1,
                marks.GetProperty(
                    "PracticalExample").GetInt32());

            Assert.Equal(
                10,
                marks.GetProperty("Total").GetInt32());

            var format = root
                .GetProperty("text")
                .GetProperty("format");

            Assert.Equal(
                "json_schema",
                format.GetProperty("type").GetString());

            Assert.Equal(
                "provisional_interview_assessment",
                format.GetProperty("name").GetString());

            Assert.True(
                format.GetProperty("strict").GetBoolean());

            var schema = format.GetProperty("schema");

            Assert.Equal(
                "object",
                schema.GetProperty("type").GetString());

            Assert.False(
                schema.GetProperty(
                    "additionalProperties").GetBoolean());

            string[] expectedProperties =
            {
                "suggestedScore",
                "technicalAccuracyScore",
                "keyConceptScore",
                "clarityScore",
                "practicalExampleScore",
                "strengths",
                "areasForImprovement",
                "improvementGuidance",
                "requiresManualReview"
            };

            var properties =
                schema.GetProperty("properties");

            var required =
                schema.GetProperty("required")
                    .EnumerateArray()
                    .Select(item => item.GetString()!)
                    .ToArray();

            Assert.Equal(
                expectedProperties.Length,
                properties.EnumerateObject().Count());

            Assert.Equal(
                expectedProperties.Length,
                required.Length);

            foreach (string property in expectedProperties)
            {
                Assert.True(
                    properties.TryGetProperty(
                        property, out _));

                Assert.Contains(property, required);
            }

            Assert.Equal(
                "boolean",
                properties.GetProperty(
                    "requiresManualReview")
                    .GetProperty("type")
                    .GetString());

            Assert.Equal(
                1200,
                root.GetProperty(
                    "max_output_tokens").GetInt32());
        }

        // Test configuration uses a fake API key.
        private static IConfiguration CreateConfiguration()
        {
            return new ConfigurationBuilder()
                .AddInMemoryCollection(
                    new Dictionary<string, string?>
                    {
                        ["OpenAI:ApiKey"] = "test-key",
                        ["OpenAI:Model"] = "gpt-4.1-mini"
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
                    QuestionText =
                        "Explain classes and objects."
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

        // Captures outgoing HTTP details without
        // contacting the actual OpenAI API.
        private sealed class FakeHttpMessageHandler
            : HttpMessageHandler
        {
            private readonly HttpStatusCode _statusCode;
            private readonly string _responseJson;

            public HttpMethod? RequestMethod { get; private set; }

            public string? RequestUri { get; private set; }

            public string? AuthorizationScheme { get; private set; }

            public string? AuthorizationParameter { get; private set; }

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

                RequestUri =
                    request.RequestUri?.ToString();

                AuthorizationScheme =
                    request.Headers.Authorization?.Scheme;

                AuthorizationParameter =
                    request.Headers.Authorization?.Parameter;

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
                Assert.Equal("OpenAI", name);
                return _client;
            }
        }
    }
}
