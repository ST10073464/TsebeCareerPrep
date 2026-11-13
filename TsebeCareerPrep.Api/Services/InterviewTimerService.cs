
using TsebeCareerPrep.Api.Models;

namespace TsebeCareerPrep.Api.Services
{
    public static class InterviewTimerService
    {
        // Maximum time credited without a heartbeat.
        public const int HeartbeatTimeoutSeconds = 30;


        public static void Start(
            InterviewSession session,
            DateTime nowUtc)
        {
            if (session.Status != "InProgress")
                return;

            if (session.IsTimerRunning)
            {
                var lastActivity =
                    session.LastTimerHeartbeatUtc
                    ?? session.TimerStartedAtUtc;

                if (lastActivity.HasValue &&
                    (nowUtc - lastActivity.Value).TotalSeconds
                        <= HeartbeatTimeoutSeconds)
                {
                    // The timer is already active.
                    return;
                }

                // Recover an abandoned timer.
                // Credit at most the heartbeat timeout.
                Pause(session, nowUtc);
            }

            session.IsTimerRunning = true;
            session.TimerStartedAtUtc = nowUtc;
            session.LastTimerHeartbeatUtc = nowUtc;
        }



        public static void Heartbeat(
            InterviewSession session,
            DateTime nowUtc)
        {
            if (session.Status != "InProgress" ||
                !session.IsTimerRunning)
                return;

            var lastActivity =
                session.LastTimerHeartbeatUtc
                ?? session.TimerStartedAtUtc;

            if (!lastActivity.HasValue ||
                (nowUtc - lastActivity.Value).TotalSeconds >
                    HeartbeatTimeoutSeconds)
            {
                // The heartbeat arrived too late.
                // Credit no more than the permitted interval,
                // then leave the timer paused.
                Pause(session, nowUtc);
                return;
            }

            AccumulateElapsedTime(session, nowUtc);

            session.TimerStartedAtUtc = nowUtc;
            session.LastTimerHeartbeatUtc = nowUtc;
        }


        public static void Pause(
            InterviewSession session,
            DateTime nowUtc)
        {
            if (!session.IsTimerRunning)
                return;

            AccumulateElapsedTime(session, nowUtc);

            session.IsTimerRunning = false;
            session.TimerStartedAtUtc = null;
            session.LastTimerHeartbeatUtc = null;
        }

        public static void Reset(InterviewSession session)
        {
            session.ActiveDurationSeconds = 0;
            session.IsTimerRunning = false;
            session.TimerStartedAtUtc = null;
            session.LastTimerHeartbeatUtc = null;
        }

        public static long GetElapsedSeconds(
            InterviewSession session,
            DateTime nowUtc)
        {
            if (!session.IsTimerRunning)
                return session.ActiveDurationSeconds;

            var elapsed = GetCreditableSeconds(
                session,
                nowUtc);

            return session.ActiveDurationSeconds + elapsed;
        }

        private static void AccumulateElapsedTime(
            InterviewSession session,
            DateTime nowUtc)
        {
            var elapsed = GetCreditableSeconds(
                session,
                nowUtc);

            session.ActiveDurationSeconds += elapsed;
        }

        private static long GetCreditableSeconds(
            InterviewSession session,
            DateTime nowUtc)
        {
            var referenceTime =
                session.LastTimerHeartbeatUtc
                ?? session.TimerStartedAtUtc;

            if (!referenceTime.HasValue)
                return 0;

            var elapsedSeconds =
                (long)Math.Floor(
                    (nowUtc - referenceTime.Value).TotalSeconds);

            if (elapsedSeconds <= 0)
                return 0;

            return Math.Min(
                elapsedSeconds,
                HeartbeatTimeoutSeconds);
        }
    }
}
