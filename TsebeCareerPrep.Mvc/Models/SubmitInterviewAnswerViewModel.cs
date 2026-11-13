
using System;
using System.ComponentModel.DataAnnotations;

namespace TsebeCareerPrep.Mvc.Models
{
    public class SubmitInterviewAnswerViewModel
    {
        [Range(1, int.MaxValue)]
        public int InterviewSessionId { get; set; }

        [Range(1, int.MaxValue)]
        public int InterviewQuestionId { get; set; }

        public string QuestionText { get; set; }
            = string.Empty;

        [Required(ErrorMessage = "Please enter your answer.")]
        [StringLength(
            5000,
            MinimumLength = 1,
            ErrorMessage = "Your answer must contain between 1 and 5000 characters.")]
        [Display(Name = "Your Answer")]
        public string AnswerText { get; set; }
            = string.Empty;

        // Interview timer properties

        public long ActiveDurationSeconds { get; set; } = 0;

        public bool IsTimerRunning { get; set; } = false;

        public DateTime? TimerStartedAtUtc { get; set; }

        public DateTime? LastTimerHeartbeatUtc { get; set; }
    }
}
