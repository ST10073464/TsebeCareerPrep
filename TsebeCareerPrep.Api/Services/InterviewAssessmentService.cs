
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;
using TsebeCareerPrep.Api.Data;
using TsebeCareerPrep.Api.Models;

namespace TsebeCareerPrep.Api.Services
{
    public class InterviewAssessmentService
        : IInterviewAssessmentService
    {
        private readonly ApplicationDbContext _context;

        public InterviewAssessmentService(
            ApplicationDbContext context)
        {
            _context = context;
        }

        // Formal scoring remains disabled.
        // Formal marks require verified technical accuracy.
        public async Task<InterviewAssessment?> AssessResponseAsync(
            int interviewResponseId)
        {
            if (interviewResponseId <= 0)
                return null;

            var response = await _context.InterviewResponses
                .AsNoTracking()
                .FirstOrDefaultAsync(r =>
                    r.Id == interviewResponseId);

            if (response == null ||
                string.IsNullOrWhiteSpace(response.AnswerText))
                return null;

            var rubric = await _context.InterviewQuestionRubrics
                .AsNoTracking()
                .FirstOrDefaultAsync(r =>
                    r.InterviewQuestionId ==
                        response.InterviewQuestionId &&
                    r.IsActive);

            if (rubric == null || !HasValidRubric(rubric))
                return null;

            // Formal scoring is deliberately unavailable.
            return null;
        }

        // Generate immediate learning feedback.
        // No formal marks are awarded or saved.
        public async Task<InterviewAssessmentFeedback?>
            GetFeedbackAsync(int interviewResponseId)
        {
            if (interviewResponseId <= 0)
                return null;

            var response = await _context.InterviewResponses
                .AsNoTracking()
                .FirstOrDefaultAsync(r =>
                    r.Id == interviewResponseId);

            if (response == null ||
                string.IsNullOrWhiteSpace(response.AnswerText))
                return null;

            var rubric = await _context.InterviewQuestionRubrics
                .AsNoTracking()
                .FirstOrDefaultAsync(r =>
                    r.InterviewQuestionId ==
                        response.InterviewQuestionId &&
                    r.IsActive);

            if (rubric == null || !HasValidRubric(rubric))
                return null;

            var evidence = DetectEvidence(response, rubric);

            int totalConcepts =
                evidence.DetectedConcepts.Count +
                evidence.MissingConcepts.Count;

            int conceptCoveragePercentage =
                totalConcepts > 0
                    ? (int)Math.Round(
                        (double)evidence.DetectedConcepts.Count
                        / totalConcepts * 100,
                        MidpointRounding.AwayFromZero)
                    : 0;


            var guidance = string.IsNullOrWhiteSpace(
                rubric.CandidateGuidance)
                ? "Review the suggested answer and consider " +
                  "how you could explain the relevant concepts " +
                  "more clearly, using an appropriate example."
                : rubric.CandidateGuidance.Trim();


            // Retrieve only active rules for this question.
            var misconceptionRules =
                await _context.InterviewMisconceptionRules
                    .AsNoTracking()
                    .Where(r =>
                        r.InterviewQuestionId ==
                            response.InterviewQuestionId &&
                        r.IsActive)
                    .ToListAsync();

            var potentialMisconceptions =
                DetectPotentialMisconceptions(
                    response.AnswerText,
                    misconceptionRules);

            string reviewReason =
                potentialMisconceptions.Count > 0
                    ? "Potential misconceptions were detected. " +
                      "The answer requires verification for " +
                      "technical accuracy and context."
                    : evidence.ReviewReason;

            return new InterviewAssessmentFeedback
            {
                InterviewResponseId = response.Id,

                InterviewQuestionId =
                    response.InterviewQuestionId,

                DetectedConcepts =
                    evidence.DetectedConcepts.ToList(),

                ConceptsToReview =
                    evidence.MissingConcepts.ToList(),

                ConceptCoveragePercentage =
                    conceptCoveragePercentage,

                PotentialMisconceptions =
                    potentialMisconceptions,

                ImprovementGuidance = guidance,

                RequiresManualReview = true,

                ReviewReason = reviewReason,

                AssessmentStatus = "Not formally scored"
            };
        }

        // Detect configured misconception phrases.
        // A match is only a warning, not a verified error.
        internal static List<string> DetectPotentialMisconceptions(
            string answer,
            IEnumerable<InterviewMisconceptionRule> rules)
        {
            var warnings = new List<string>();

            if (string.IsNullOrWhiteSpace(answer))
                return warnings;

            foreach (var rule in rules)
            {
                if (!rule.IsActive ||
                    string.IsNullOrWhiteSpace(rule.TriggerPhrase) ||
                    string.IsNullOrWhiteSpace(rule.FeedbackMessage))
                    continue;

                // Inspect each occurrence independently. A rejected
                // misconception must not suppress a separate assertion.
                string pattern = BuildWholePhrasePattern(rule.TriggerPhrase);
                foreach (Match match in Regex.Matches(
                    answer,
                    pattern,
                    RegexOptions.IgnoreCase | RegexOptions.CultureInvariant,
                    TimeSpan.FromMilliseconds(250)))
                {
                    if (IsExplicitlyRejected(answer, match.Index))
                        continue;

                    string message = rule.FeedbackMessage.Trim();
                    if (!warnings.Contains(
                        message,
                        StringComparer.OrdinalIgnoreCase))
                    {
                        warnings.Add(message);
                    }
                }
            }

            return warnings;
        }

        // Limited negation safeguard for explicit rejection immediately
        // before a trigger. This is not semantic understanding.
        private static bool IsExplicitlyRejected(string answer, int matchIndex)
        {
            // Keep the check within the current sentence or clause.
            int start = matchIndex;
            while (start > 0 &&
                   answer[start - 1] != '.' &&
                   answer[start - 1] != '!' &&
                   answer[start - 1] != '?' &&
                   answer[start - 1] != ';' &&
                   answer[start - 1] != '\n')
            {
                start--;
            }

            string prefix = answer[start..matchIndex].Trim();
            return Regex.IsMatch(
                prefix,
                @"(?:\bit\s+is\s+(?:false|incorrect|wrong|untrue)\s+that|" +
                @"\bit\s+is\s+a\s+(?:misconception|myth)\s+that|" +
                @"\bit\s+is\s+wrong\s+to\s+say\s+that|" +
                @"\b(?:false|incorrect|wrong)\s+to\s+(?:say|claim)\s+that)\s*$",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant,
                TimeSpan.FromMilliseconds(250));
        }

        private static string BuildWholePhrasePattern(string phrase)
        {
            return @"(?<![\p{L}\p{N}_])" +
                   Regex.Escape(phrase.Trim()) +
                   @"(?![\p{L}\p{N}_])";
        }

        // Validate the existing 10-mark rubric.
        private static bool HasValidRubric(
            InterviewQuestionRubric rubric)
        {
            if (rubric.TechnicalAccuracyMarks < 0 ||
                rubric.KeyConceptMarks < 0 ||
                rubric.ClarityMarks < 0 ||
                rubric.PracticalExampleMarks < 0)
                return false;

            int totalAvailableMarks =
                rubric.TechnicalAccuracyMarks +
                rubric.KeyConceptMarks +
                rubric.ClarityMarks +
                rubric.PracticalExampleMarks;

            return totalAvailableMarks == 10 &&
                !string.IsNullOrWhiteSpace(
                    rubric.ExpectedConcepts);
        }

        // Detect expected concepts and their explicitly
        // mapped acceptable alternatives.
        internal static InterviewAssessmentEvidence DetectEvidence(
            InterviewResponse response,
            InterviewQuestionRubric rubric)
        {
            var evidence = new InterviewAssessmentEvidence
            {
                InterviewResponseId = response.Id,

                InterviewQuestionId =
                    response.InterviewQuestionId,

                RequiresManualReview = true,

                ReviewReason =
                    "Detected phrases require verification " +
                    "for accuracy and context."
            };

            var concepts = rubric.ExpectedConcepts
                .Split(
                    ';',
                    StringSplitOptions.RemoveEmptyEntries |
                    StringSplitOptions.TrimEntries)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var alternativeMappings =
                ParseAlternativeMappings(
                    rubric.AcceptableAlternatives,
                    concepts);

            foreach (var concept in concepts)
            {
                bool detected = ContainsWholePhrase(
                    response.AnswerText,
                    concept);

                if (!detected &&
                    alternativeMappings.TryGetValue(
                        concept,
                        out var alternatives))
                {
                    detected = alternatives.Any(
                        alternative => ContainsWholePhrase(
                            response.AnswerText,
                            alternative));
                }

                if (detected)
                    evidence.DetectedConcepts.Add(concept);
                else
                    evidence.MissingConcepts.Add(concept);
            }

            return evidence;
        }

        // Supported format:
        // Classes = class | class blueprint
        // Objects = object | instances
        //
        // Legacy unmapped alternatives remain stored
        // but do not count as detected concepts.
        private static Dictionary<string, List<string>>
            ParseAlternativeMappings(
                string? configuredAlternatives,
                IEnumerable<string> expectedConcepts)
        {
            var mappings =
                new Dictionary<string, List<string>>(
                    StringComparer.OrdinalIgnoreCase);

            if (string.IsNullOrWhiteSpace(
                configuredAlternatives))
                return mappings;

            var validConcepts = expectedConcepts
                .ToDictionary(
                    concept => concept,
                    concept => concept,
                    StringComparer.OrdinalIgnoreCase);

            var lines = configuredAlternatives.Split(
                new[] { "\r\n", "\n", "\r" },
                StringSplitOptions.RemoveEmptyEntries |
                StringSplitOptions.TrimEntries);

            foreach (var line in lines)
            {
                int separatorIndex = line.IndexOf('=');

                if (separatorIndex <= 0)
                    continue;

                string concept =
                    line[..separatorIndex].Trim();

                string alternativesText =
                    line[(separatorIndex + 1)..].Trim();

                if (!validConcepts.TryGetValue(
                    concept,
                    out var expectedConcept))
                    continue;

                if (string.IsNullOrWhiteSpace(
                    alternativesText))
                    continue;

                var alternatives = alternativesText
                    .Split(
                        '|',
                        StringSplitOptions.RemoveEmptyEntries |
                        StringSplitOptions.TrimEntries)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                if (alternatives.Count == 0)
                    continue;

                if (!mappings.TryGetValue(
                    expectedConcept,
                    out var existing))
                {
                    existing = new List<string>();
                    mappings[expectedConcept] = existing;
                }

                foreach (var alternative in alternatives)
                {
                    if (!existing.Contains(
                        alternative,
                        StringComparer.OrdinalIgnoreCase))
                    {
                        existing.Add(alternative);
                    }
                }
            }

            return mappings;
        }

        // Whole-phrase matching prevents partial-word matches.
        // Matching is case-insensitive.
        private static bool ContainsWholePhrase(
            string answer,
            string phrase)
        {
            if (string.IsNullOrWhiteSpace(answer) ||
                string.IsNullOrWhiteSpace(phrase))
                return false;

            string pattern = BuildWholePhrasePattern(phrase);

            return Regex.IsMatch(
                answer,
                pattern,
                RegexOptions.IgnoreCase |
                RegexOptions.CultureInvariant,
                TimeSpan.FromMilliseconds(250));
        }
    }
}
