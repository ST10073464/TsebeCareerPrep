
using Microsoft.EntityFrameworkCore;
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

        // Formal scoring is not yet implemented.
        // Retain the existing validation and safety rules.
        public async Task<InterviewAssessment?> AssessResponseAsync(
            int interviewResponseId)
        {
            if (interviewResponseId <= 0)
                return null;

            var response = await _context.InterviewResponses
                .AsNoTracking()
                .FirstOrDefaultAsync(r =>
                    r.Id == interviewResponseId);

            if (response == null)
                return null;

            var rubric = await _context.InterviewQuestionRubrics
                .AsNoTracking()
                .FirstOrDefaultAsync(r =>
                    r.InterviewQuestionId ==
                        response.InterviewQuestionId &&
                    r.IsActive);

            if (rubric == null)
                return null;

            if (string.IsNullOrWhiteSpace(response.AnswerText))
                return null;

            if (!HasValidRubric(rubric))
                return null;

            var evidence = DetectEvidence(response, rubric);

            if (evidence.RequiresManualReview)
                return null;

            return null;
        }

        // Generate indicative learning feedback.
        // No marks are awarded and no assessment is saved.
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
            {
                return null;
            }

            var rubric = await _context.InterviewQuestionRubrics
                .AsNoTracking()
                .FirstOrDefaultAsync(r =>
                    r.InterviewQuestionId ==
                        response.InterviewQuestionId &&
                    r.IsActive);

            if (rubric == null || !HasValidRubric(rubric))
                return null;

            var evidence = DetectEvidence(response, rubric);

            var guidance = string.IsNullOrWhiteSpace(
                rubric.AssessmentGuidance)
                ? "Review the suggested answer and consider " +
                  "how you could explain the relevant concepts " +
                  "more clearly, using an appropriate example."
                : rubric.AssessmentGuidance.Trim();

            return new InterviewAssessmentFeedback
            {
                InterviewResponseId = response.Id,
                InterviewQuestionId =
                    response.InterviewQuestionId,

                DetectedConcepts =
                    evidence.DetectedConcepts.ToList(),

                ConceptsToReview =
                    evidence.MissingConcepts.ToList(),

                ImprovementGuidance = guidance,

                RequiresManualReview =
                    evidence.RequiresManualReview,

                ReviewReason = evidence.ReviewReason,

                AssessmentStatus = "Not formally scored"
            };
        }

        // Validate the configured 10-mark rubric.
        private static bool HasValidRubric(
            InterviewQuestionRubric rubric)
        {
            if (rubric.TechnicalAccuracyMarks < 0 ||
                rubric.KeyConceptMarks < 0 ||
                rubric.ClarityMarks < 0 ||
                rubric.PracticalExampleMarks < 0)
            {
                return false;
            }

            int totalAvailableMarks =
                rubric.TechnicalAccuracyMarks +
                rubric.KeyConceptMarks +
                rubric.ClarityMarks +
                rubric.PracticalExampleMarks;

            if (totalAvailableMarks != 10)
                return false;

            if (string.IsNullOrWhiteSpace(
                rubric.ExpectedConcepts))
            {
                return false;
            }

            return true;
        }

        // Detect possible assessment evidence in an answer.
        // Detection does not establish correctness.
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
                .Distinct(StringComparer.OrdinalIgnoreCase);

            foreach (var concept in concepts)
            {
                // Literal phrase matching only.
                bool detected = response.AnswerText.Contains(
                    concept,
                    StringComparison.OrdinalIgnoreCase);

                if (detected)
                {
                    evidence.DetectedConcepts.Add(concept);
                }
                else
                {
                    evidence.MissingConcepts.Add(concept);
                }
            }

            return evidence;
        }
    }
}
