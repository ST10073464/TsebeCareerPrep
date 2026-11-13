
using System.Text.Json;
using TsebeCareerPrep.Api.Models;

namespace TsebeCareerPrep.Api.Services
{
    public static class OpenAiAssessmentResponseParser
    {
        public static ProvisionalInterviewAssessment? Parse(
            string responseJson,
            InterviewQuestionRubric rubric)
        {
            if (string.IsNullOrWhiteSpace(responseJson) ||
                rubric == null)
            {
                return null;
            }

            try
            {
                using var document =
                    JsonDocument.Parse(responseJson);

                var root = document.RootElement;

                if (root.ValueKind != JsonValueKind.Object)
                {
                    return null;
                }

                // Only accept completed OpenAI responses.
                if (!root.TryGetProperty(
                        "status", out var status) ||
                    status.ValueKind != JsonValueKind.String ||
                    status.GetString() != "completed")
                {
                    return null;
                }

                if (!root.TryGetProperty(
                        "output", out var output) ||
                    output.ValueKind != JsonValueKind.Array)
                {
                    return null;
                }

                string? assessmentJson = null;

                foreach (var item in output.EnumerateArray())
                {
                    if (item.ValueKind != JsonValueKind.Object ||
                        !item.TryGetProperty(
                            "type", out var itemType) ||
                        itemType.ValueKind != JsonValueKind.String ||
                        itemType.GetString() != "message")
                    {
                        continue;
                    }

                    // Reject incomplete output messages.
                    if (item.TryGetProperty(
                            "status", out var messageStatus) &&
                        (messageStatus.ValueKind !=
                            JsonValueKind.String ||
                         messageStatus.GetString() != "completed"))
                    {
                        return null;
                    }

                    if (!item.TryGetProperty(
                            "content", out var content) ||
                        content.ValueKind != JsonValueKind.Array)
                    {
                        return null;
                    }

                    foreach (var part in content.EnumerateArray())
                    {
                        if (part.ValueKind != JsonValueKind.Object ||
                            !part.TryGetProperty(
                                "type", out var partType) ||
                            partType.ValueKind != JsonValueKind.String)
                        {
                            return null;
                        }

                        string? type = partType.GetString();

                        // Never accept a refusal as an assessment.
                        if (type == "refusal")
                        {
                            return null;
                        }

                        if (type != "output_text")
                        {
                            continue;
                        }

                        if (!part.TryGetProperty(
                                "text", out var textElement) ||
                            textElement.ValueKind !=
                                JsonValueKind.String)
                        {
                            return null;
                        }

                        // Reject multiple assessment outputs.
                        if (assessmentJson != null)
                        {
                            return null;
                        }

                        assessmentJson = textElement.GetString();
                    }
                }

                if (string.IsNullOrWhiteSpace(assessmentJson))
                {
                    return null;
                }

                // Verify required fields and their JSON types.
                if (!HasRequiredProperties(assessmentJson))
                {
                    return null;
                }

                var assessment =
                    JsonSerializer.Deserialize<
                        ProvisionalInterviewAssessment>(
                        assessmentJson,
                        new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });

                if (assessment == null ||
                    !assessment.RequiresManualReview)
                {
                    return null;
                }

                // Validate scores against the actual rubric.
                return AiAssessmentValidationService.IsValid(
                    assessment,
                    rubric)
                    ? assessment
                    : null;
            }
            catch (JsonException)
            {
                return null;
            }
            catch (InvalidOperationException)
            {
                return null;
            }
        }

        private static bool HasRequiredProperties(
            string assessmentJson)
        {
            using var document =
                JsonDocument.Parse(assessmentJson);

            var root = document.RootElement;

            if (root.ValueKind != JsonValueKind.Object)
            {
                return false;
            }

            string[] integerProperties =
            {
                "suggestedScore",
                "technicalAccuracyScore",
                "keyConceptScore",
                "clarityScore",
                "practicalExampleScore"
            };

            string[] stringProperties =
            {
                "strengths",
                "areasForImprovement",
                "improvementGuidance"
            };

            // Validate all five score properties.
            foreach (string property in integerProperties)
            {
                if (!root.TryGetProperty(
                        property, out var value) ||
                    value.ValueKind != JsonValueKind.Number ||
                    !value.TryGetInt32(out _))
                {
                    return false;
                }
            }

            // Validate all three feedback properties.
            foreach (string property in stringProperties)
            {
                if (!root.TryGetProperty(
                        property, out var value) ||
                    value.ValueKind != JsonValueKind.String)
                {
                    return false;
                }
            }

            // Human review must always be required.
            if (!root.TryGetProperty(
                    "requiresManualReview", out var review) ||
                review.ValueKind != JsonValueKind.True)
            {
                return false;
            }

            // Reject additional or duplicate properties.
            string[] allowedProperties =
                integerProperties
                    .Concat(stringProperties)
                    .Append("requiresManualReview")
                    .ToArray();

            var actualProperties =
                root.EnumerateObject()
                    .Select(property => property.Name)
                    .ToArray();

            if (actualProperties.Length !=
                allowedProperties.Length)
            {
                return false;
            }

            if (actualProperties.Distinct(
                    StringComparer.Ordinal).Count() !=
                actualProperties.Length)
            {
                return false;
            }

            if (actualProperties.Any(
                    property => !allowedProperties.Contains(
                        property, StringComparer.Ordinal)))
            {
                return false;
            }

            return true;
        }
    }
}
