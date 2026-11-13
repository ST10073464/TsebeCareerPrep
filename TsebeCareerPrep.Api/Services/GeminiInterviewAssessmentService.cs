
using System.Net.Http.Json;
using System.Text.Json;
using TsebeCareerPrep.Api.Models;

namespace TsebeCareerPrep.Api.Services
{
    public class GeminiInterviewAssessmentService
        : IAiInterviewAssessmentService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;

        public GeminiInterviewAssessmentService(
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
        }

        public async Task<ProvisionalInterviewAssessment?>
            GenerateProvisionalAssessmentAsync(
                InterviewResponse response,
                InterviewQuestionRubric rubric,
                CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(response);
            ArgumentNullException.ThrowIfNull(rubric);

            cancellationToken.ThrowIfCancellationRequested();

            string? questionText =
                response.InterviewQuestion?.QuestionText;

            if (string.IsNullOrWhiteSpace(questionText) ||
                string.IsNullOrWhiteSpace(response.AnswerText) ||
                !rubric.IsActive)
            {
                return null;
            }

            int totalMarks =
                rubric.TechnicalAccuracyMarks +
                rubric.KeyConceptMarks +
                rubric.ClarityMarks +
                rubric.PracticalExampleMarks;

            if (rubric.TechnicalAccuracyMarks < 0 ||
                rubric.KeyConceptMarks < 0 ||
                rubric.ClarityMarks < 0 ||
                rubric.PracticalExampleMarks < 0 ||
                totalMarks != 10)
            {
                return null;
            }

            string model =
                _configuration["Gemini:Model"]
                ?? "gemini-2.5-flash";

            string? apiKey =
                _configuration["Gemini:ApiKey"];

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                throw new InvalidOperationException(
                    "Gemini API key is not configured.");
            }

            // Reuse the existing strict assessment instructions.
            var existingRequest =
                OpenAiAssessmentRequestBuilder.Build(
                    response,
                    rubric,
                    model,
                    questionText);

            using var requestDocument =
                JsonDocument.Parse(
                    JsonSerializer.Serialize(existingRequest));

            string instructions = requestDocument.RootElement
                .GetProperty("instructions")
                .GetString()!;

            string assessmentInput = requestDocument.RootElement
                .GetProperty("input")
                .GetString()!;

            // Gemini uses generateContent rather than
            // OpenAI's Responses API endpoint.
            var requestBody = new
            {
                systemInstruction = new
                {
                    parts = new[]
                    {
                        new { text = instructions }
                    }
                },

                contents = new[]
                {
                    new
                    {
                        role = "user",
                        parts = new[]
                        {
                            new { text = assessmentInput }
                        }
                    }
                },

                generationConfig = new
                {
                    temperature = 0,
                    maxOutputTokens = 1200,
                    responseMimeType = "application/json"
                }
            };

            using var client =
                _httpClientFactory.CreateClient("Gemini");

            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                $"models/{Uri.EscapeDataString(model)}:generateContent");

            // Keep the API key out of the URL.
            request.Headers.Add("x-goog-api-key", apiKey);
            request.Content = JsonContent.Create(requestBody);

            try
            {
                using var httpResponse =
                    await client.SendAsync(
                        request,
                        HttpCompletionOption.ResponseHeadersRead,
                        cancellationToken);

                if (!httpResponse.IsSuccessStatusCode)
                {
                    return null;
                }

                string responseJson =
                    await httpResponse.Content.ReadAsStringAsync(
                        cancellationToken);

                using var document =
                    JsonDocument.Parse(responseJson);

                // Gemini returns generated text inside
                // candidates[].content.parts[].text.
                if (!document.RootElement.TryGetProperty(
                    "candidates", out var candidates) ||
                    candidates.ValueKind != JsonValueKind.Array ||
                    candidates.GetArrayLength() == 0)
                {
                    return null;
                }

                var candidate = candidates[0];

                if (!candidate.TryGetProperty(
                    "content", out var content) ||
                    !content.TryGetProperty(
                    "parts", out var parts) ||
                    parts.ValueKind != JsonValueKind.Array ||
                    parts.GetArrayLength() == 0)
                {
                    return null;
                }

                string? assessmentJson = null;

                foreach (var part in parts.EnumerateArray())
                {
                    if (part.TryGetProperty(
                        "text", out var textElement))
                    {
                        assessmentJson =
                            textElement.GetString();

                        break;
                    }
                }

                if (string.IsNullOrWhiteSpace(assessmentJson))
                {
                    return null;
                }

                // Adapt Gemini's JSON output to the
                // already-tested OpenAI response parser.
                var compatibleResponse = new
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

                return OpenAiAssessmentResponseParser.Parse(
                    JsonSerializer.Serialize(compatibleResponse),
                    rubric);
            }
            catch (HttpRequestException)
            {
                return null;
            }
            catch (JsonException)
            {
                return null;
            }
            catch (TaskCanceledException)
                when (!cancellationToken.IsCancellationRequested)
            {
                return null;
            }
        }
    }
}
