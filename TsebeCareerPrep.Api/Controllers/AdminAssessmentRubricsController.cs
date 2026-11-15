
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using TsebeCareerPrep.Api.Data;
using TsebeCareerPrep.Api.Models;

namespace TsebeCareerPrep.Api.Controllers
{
    [ApiController]
    [Authorize(Roles = "Administrator")]
    [Route("api/admin/assessment-rubrics")]
    public class AdminAssessmentRubricsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public AdminAssessmentRubricsController(
            ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/admin/assessment-rubrics/question/5
        [HttpGet("question/{questionId:int}")]
        public async Task<IActionResult> GetByQuestion(
            int questionId)
        {
            if (questionId <= 0)
                return BadRequest("Invalid question ID.");

            var questionExists = await _context.InterviewQuestions
                .AnyAsync(q => q.Id == questionId);

            if (!questionExists)
                return NotFound("Interview question not found.");

            var rubric = await _context.InterviewQuestionRubrics
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    r => r.InterviewQuestionId == questionId);

            if (rubric == null)
                return NotFound("No rubric configured for this question.");

            return Ok(rubric);
        }

        // PUT: api/admin/assessment-rubrics/question/5
        // Creates or updates the question's rubric.
        [HttpPut("question/{questionId:int}")]
        public async Task<IActionResult> Save(
            int questionId,
            [FromBody] SaveRubricRequest request)
        {
            if (questionId <= 0)
                return BadRequest("Invalid question ID.");

            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            var questionExists = await _context.InterviewQuestions
                .AnyAsync(q => q.Id == questionId);

            if (!questionExists)
                return NotFound("Interview question not found.");

            var concepts = request.ExpectedConcepts
                .Split(';',
                    StringSplitOptions.RemoveEmptyEntries |
                    StringSplitOptions.TrimEntries)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (concepts.Count == 0)
                return BadRequest(
                    "At least one expected concept is required.");

            int totalMarks =
                request.TechnicalAccuracyMarks +
                request.KeyConceptMarks +
                request.ClarityMarks +
                request.PracticalExampleMarks;

            if (totalMarks != 10)
                return BadRequest(
                    "The rubric criteria must total 10 marks.");

            if (request.TechnicalAccuracyMarks < 0 ||
                request.KeyConceptMarks < 0 ||
                request.ClarityMarks < 0 ||
                request.PracticalExampleMarks < 0)
            {
                return BadRequest(
                    "Rubric criteria cannot have negative marks.");
            }

            var rubric = await _context.InterviewQuestionRubrics
                .FirstOrDefaultAsync(
                    r => r.InterviewQuestionId == questionId);

            bool isNew = rubric == null;

            if (isNew)
            {
                rubric = new InterviewQuestionRubric
                {
                    InterviewQuestionId = questionId
                };

                _context.InterviewQuestionRubrics.Add(rubric);
            }

            rubric!.ExpectedConcepts =
                string.Join("; ", concepts);

            rubric.AcceptableAlternatives =
                request.AcceptableAlternatives.Trim();

            rubric.AssessmentGuidance =
                request.AssessmentGuidance.Trim();

            rubric.CandidateGuidance =
                request.CandidateGuidance.Trim();

            rubric.TechnicalAccuracyMarks =
                request.TechnicalAccuracyMarks;

            rubric.KeyConceptMarks =
                request.KeyConceptMarks;

            rubric.ClarityMarks =
                request.ClarityMarks;

            rubric.PracticalExampleMarks =
                request.PracticalExampleMarks;

            rubric.IsActive = request.IsActive;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = isNew
                    ? "Assessment rubric created successfully."
                    : "Assessment rubric updated successfully.",
                rubric.Id,
                rubric.InterviewQuestionId,
                rubric.IsActive,
                FormalScoringEnabled = false
            });
        }
    }

    public class SaveRubricRequest
    {
        [Required]
        [MaxLength(2000)]
        public string ExpectedConcepts { get; set; }
            = string.Empty;

        [MaxLength(2000)]
        public string AcceptableAlternatives { get; set; }
            = string.Empty;

        [MaxLength(2000)]
        public string AssessmentGuidance { get; set; }
            = string.Empty;

        [MaxLength(2000)]
        public string CandidateGuidance { get; set; }
            = string.Empty;

        [Range(0, 10)]
        public int TechnicalAccuracyMarks { get; set; } = 4;

        [Range(0, 10)]
        public int KeyConceptMarks { get; set; } = 3;

        [Range(0, 10)]
        public int ClarityMarks { get; set; } = 2;

        [Range(0, 10)]
        public int PracticalExampleMarks { get; set; } = 1;

        public bool IsActive { get; set; } = true;
    }
}
