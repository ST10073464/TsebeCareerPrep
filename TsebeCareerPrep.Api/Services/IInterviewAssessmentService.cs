
using TsebeCareerPrep.Api.Models;

namespace TsebeCareerPrep.Api.Services
{
    public interface IInterviewAssessmentService
    {
        Task<InterviewAssessment?> AssessResponseAsync(
            int interviewResponseId);

        Task<InterviewAssessmentFeedback?> GetFeedbackAsync(
            int interviewResponseId);
    }
}
