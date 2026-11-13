
using System.Net.Http.Headers;
using System.Net.Http.Json;
using TsebeCareerPrep.Api.Models;

namespace TsebeCareerPrep.Api.Services
{
    public class AiInterviewAssessmentService
        : IAiInterviewAssessmentService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;

        public AiInterviewAssessmentService(
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

            // The question must already be loaded from
            // the database before assessment begins.
            string? questionText =
                response.InterviewQuestion?.QuestionText;

            if (string.IsNullOrWhiteSpace(questionText) ||
                string.IsNullOrWhiteSpace(response.AnswerText) ||
                !rubric.IsActive)
            {
                return null;
            }

            string model =
                _configuration["OpenAI:Model"]
                ?? "gpt-4.1-mini";

            // Build the structured assessment request.
            var requestBody =
                OpenAiAssessmentRequestBuilder.Build(
                    response,
                    rubric,
                    model,
                    questionText);

            using var client =
                _httpClientFactory.CreateClient("OpenAI");

            using var request =
                CreateOpenAiRequest(requestBody);

            try
            {
                // Send the assessment request to OpenAI.
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

                // Parse and validate the provisional result.
                return OpenAiAssessmentResponseParser.Parse(
                    responseJson,
                    rubric);
            }
            catch (HttpRequestException)
            {
                return null;
            }
            catch (TaskCanceledException)
                when (!cancellationToken.IsCancellationRequested)
            {
                // HTTP timeout: no assessment is generated.
                return null;
            }
        }

        private HttpRequestMessage CreateOpenAiRequest(
            object requestBody)
        {
            string apiKey =
                _configuration["OpenAI:ApiKey"]
                ?? throw new InvalidOperationException(
                    "OpenAI API key is not configured.");

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                throw new InvalidOperationException(
                    "OpenAI API key is empty.");
            }

            var request = new HttpRequestMessage(
                HttpMethod.Post,
                "responses");

            request.Headers.Authorization =
                new AuthenticationHeaderValue(
                    "Bearer",
                    apiKey);

            request.Content = JsonContent.Create(requestBody);

            return request;
        }
    }
}
