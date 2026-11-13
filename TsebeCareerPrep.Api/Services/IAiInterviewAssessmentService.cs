
using TsebeCareerPrep.Api.Models;

namespace TsebeCareerPrep.Api.Services
{
    public interface IAiInterviewAssessmentService
    {
        Task<ProvisionalInterviewAssessment?>
            GenerateProvisionalAssessmentAsync(
                InterviewResponse response,
                InterviewQuestionRubric rubric,
                CancellationToken cancellationToken = default);
    }
}
