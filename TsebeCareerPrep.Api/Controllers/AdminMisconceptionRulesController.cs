
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
    [Route("api/admin/misconception-rules")]
    public class AdminMisconceptionRulesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public AdminMisconceptionRulesController(
            ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/admin/misconception-rules/question/5
        [HttpGet("question/{questionId:int}")]
        public async Task<IActionResult> GetByQuestion(
            int questionId)
        {
            if (questionId <= 0)
                return BadRequest("Invalid question ID.");

            bool questionExists = await _context.InterviewQuestions
                .AnyAsync(q => q.Id == questionId);

            if (!questionExists)
                return NotFound("Interview question not found.");

            var rules = await _context.InterviewMisconceptionRules
                .AsNoTracking()
                .Where(r => r.InterviewQuestionId == questionId)
                .OrderBy(r => r.Id)
                .Select(r => new
                {
                    r.Id,
                    r.InterviewQuestionId,
                    r.TriggerPhrase,
                    r.FeedbackMessage,
                    r.IsActive,
                    r.CreatedAt,
                    r.UpdatedAt
                })
                .ToListAsync();

            return Ok(rules);
        }

        // POST: api/admin/misconception-rules
        [HttpPost]
        public async Task<IActionResult> Create(
            [FromBody] SaveMisconceptionRuleRequest request)
        {
            bool questionExists = await _context.InterviewQuestions
                .AnyAsync(q => q.Id == request.InterviewQuestionId);

            if (!questionExists)
                return NotFound("Interview question not found.");

            string phrase = request.TriggerPhrase.Trim();

            bool duplicate = await _context.InterviewMisconceptionRules
                .AnyAsync(r =>
                    r.InterviewQuestionId == request.InterviewQuestionId &&
                    r.TriggerPhrase.ToLower() == phrase.ToLower());

            if (duplicate)
                return Conflict(
                    "A rule with this trigger phrase already exists for this question.");

            var rule = new InterviewMisconceptionRule
            {
                InterviewQuestionId = request.InterviewQuestionId,
                TriggerPhrase = phrase,
                FeedbackMessage = request.FeedbackMessage.Trim(),
                IsActive = request.IsActive,
                CreatedAt = DateTime.UtcNow
            };

            _context.InterviewMisconceptionRules.Add(rule);
            await _context.SaveChangesAsync();

            return CreatedAtAction(
                nameof(GetById),
                new { id = rule.Id },
                new
                {
                    rule.Id,
                    rule.InterviewQuestionId,
                    rule.TriggerPhrase,
                    rule.FeedbackMessage,
                    rule.IsActive
                });
        }

        // GET: api/admin/misconception-rules/5
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var rule = await _context.InterviewMisconceptionRules
                .AsNoTracking()
                .Where(r => r.Id == id)
                .Select(r => new
                {
                    r.Id,
                    r.InterviewQuestionId,
                    r.TriggerPhrase,
                    r.FeedbackMessage,
                    r.IsActive,
                    r.CreatedAt,
                    r.UpdatedAt
                })
                .FirstOrDefaultAsync();

            return rule == null ? NotFound() : Ok(rule);
        }

        // PUT: api/admin/misconception-rules/5
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(
            int id,
            [FromBody] SaveMisconceptionRuleRequest request)
        {
            var rule = await _context.InterviewMisconceptionRules
                .FirstOrDefaultAsync(r => r.Id == id);

            if (rule == null)
                return NotFound("Misconception rule not found.");

            // A rule must remain linked to its original question.
            if (rule.InterviewQuestionId != request.InterviewQuestionId)
                return BadRequest(
                    "The rule cannot be moved to another question.");

            string phrase = request.TriggerPhrase.Trim();

            bool duplicate = await _context.InterviewMisconceptionRules
                .AnyAsync(r =>
                    r.Id != id &&
                    r.InterviewQuestionId == rule.InterviewQuestionId &&
                    r.TriggerPhrase.ToLower() == phrase.ToLower());

            if (duplicate)
                return Conflict(
                    "Another rule already uses this trigger phrase.");

            rule.TriggerPhrase = phrase;
            rule.FeedbackMessage = request.FeedbackMessage.Trim();
            rule.IsActive = request.IsActive;
            rule.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Misconception rule updated successfully.",
                rule.Id,
                rule.IsActive
            });
        }

        // PATCH: api/admin/misconception-rules/5/status
        [HttpPatch("{id:int}/status")]
        public async Task<IActionResult> ChangeStatus(
            int id,
            [FromBody] ChangeRuleStatusRequest request)
        {
            var rule = await _context.InterviewMisconceptionRules
                .FirstOrDefaultAsync(r => r.Id == id);

            if (rule == null)
                return NotFound("Misconception rule not found.");

            rule.IsActive = request.IsActive!.Value;
            rule.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = rule.IsActive
                    ? "Misconception rule activated."
                    : "Misconception rule deactivated.",
                rule.Id,
                rule.IsActive
            });
        }
    }

    public class SaveMisconceptionRuleRequest
    {
        [Range(1, int.MaxValue)]
        public int InterviewQuestionId { get; set; }

        [Required]
        [StringLength(500, MinimumLength = 1)]
        [RegularExpression(@".*\S.*",
            ErrorMessage = "A trigger phrase is required.")]
        public string TriggerPhrase { get; set; } = string.Empty;

        [Required]
        [StringLength(2000, MinimumLength = 1)]
        [RegularExpression(@"[\s\S]*\S[\s\S]*",
            ErrorMessage = "A feedback message is required.")]
        public string FeedbackMessage { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;
    }

    public class ChangeRuleStatusRequest
    {
        [Required]
        public bool? IsActive { get; set; }
    }
}
