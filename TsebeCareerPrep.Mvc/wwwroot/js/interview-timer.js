
(function () {
    "use strict";

    document.addEventListener("DOMContentLoaded", function () {
        const timerElement = document.getElementById("interviewTimer");
        const statusElement = document.getElementById("interviewTimerStatus");

        if (!timerElement || !statusElement) {
            return;
        }

        const heartbeatUrl = timerElement.dataset.heartbeatUrl;
        const token = document.querySelector(
            '#interviewTimerSecurityForm input[name="__RequestVerificationToken"]'
        )?.value;

        let savedSeconds = Math.max(
            0,
            Number(timerElement.dataset.elapsedSeconds) || 0
        );

        let isRunning = timerElement.dataset.running === "true";
        let lastSyncTime = performance.now();
        let heartbeatInProgress = false;

        // Include up to 30 seconds accrued since the last
        // server heartbeat, without counting unlimited idle time.
        if (isRunning) {
            const lastHeartbeat =
                timerElement.dataset.lastHeartbeat ||
                timerElement.dataset.startedAt;

            if (lastHeartbeat) {
                const elapsed = Math.max(
                    0,
                    (Date.now() - Date.parse(lastHeartbeat)) / 1000
                );

                if (Number.isFinite(elapsed)) {
                    savedSeconds += Math.min(30, Math.floor(elapsed));
                }
            }
        }

        function formatDuration(totalSeconds) {
            const seconds = Math.max(0, Math.floor(totalSeconds));
            const hours = Math.floor(seconds / 3600);
            const minutes = Math.floor((seconds % 3600) / 60);
            const remainder = seconds % 60;

            return [
                String(hours).padStart(2, "0"),
                String(minutes).padStart(2, "0"),
                String(remainder).padStart(2, "0")
            ].join(":");
        }

        function updateDisplay() {
            const elapsed = savedSeconds + (
                isRunning
                    ? Math.floor((performance.now() - lastSyncTime) / 1000)
                    : 0
            );

            timerElement.textContent = formatDuration(elapsed);
            statusElement.textContent = isRunning ? "Running" : "Paused";

            statusElement.classList.toggle("bg-success", isRunning);
            statusElement.classList.toggle("bg-secondary", !isRunning);
        }

        async function sendHeartbeat() {
            if (!isRunning || heartbeatInProgress) {
                return;
            }

            if (!heartbeatUrl || !token) {
                isRunning = false;
                updateDisplay();
                console.error("Interview timer configuration is missing.");
                return;
            }

            heartbeatInProgress = true;

            try {
                const response = await fetch(heartbeatUrl, {
                    method: "POST",
                    headers: {
                        "RequestVerificationToken": token
                    },
                    credentials: "same-origin",
                    cache: "no-store"
                });

                if (!response.ok) {
                    isRunning = false;
                    updateDisplay();
                    console.error(
                        "Interview heartbeat returned:",
                        response.status
                    );
                    return;
                }

                const result = await response.json();

                savedSeconds = Math.max(
                    0,
                    Number(result.activeDurationSeconds) || 0
                );

                isRunning = result.isTimerRunning === true;
                lastSyncTime = performance.now();

                updateDisplay();
            } catch (error) {
                isRunning = false;
                updateDisplay();
                console.error("Interview heartbeat failed:", error);
            } finally {
                heartbeatInProgress = false;
            }
        }

        updateDisplay();

        setInterval(updateDisplay, 1000);
        setInterval(sendHeartbeat, 10000);
    });
})();
