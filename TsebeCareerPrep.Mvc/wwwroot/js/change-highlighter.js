
/*
 * Tsebe CareerPrep
 * Project-wide Change Highlighting
 * Supports desktop and mobile layouts.
 */
(function () {
    "use strict";

    const duration = 5000;
    const types = ["created", "updated", "status", "error"];

    const reducedMotion = window.matchMedia(
        "(prefers-reduced-motion: reduce)"
    );

    function isVisible(element) {
        if (!element || !element.isConnected) return false;

        const style = window.getComputedStyle(element);

        return style.display !== "none" &&
            style.visibility !== "hidden" &&
            element.getClientRects().length > 0;
    }

    function highlight(element, type = "updated") {
        if (!element) return false;

        if (!types.includes(type)) {
            type = "updated";
        }

        types.forEach(t => {
            element.classList.remove("tcp-highlight-" + t);
        });

        // Restart the highlight animation.
        void element.offsetWidth;

        element.classList.add("tcp-highlight-" + type);

        element.scrollIntoView({
            behavior: reducedMotion.matches ? "auto" : "smooth",
            block: "center"
        });

        window.setTimeout(function () {
            element.classList.remove("tcp-highlight-" + type);
        }, duration);

        return true;
    }

    function findTarget(id) {
        const key = String(id);

        const candidates = Array.from(
            document.querySelectorAll("[data-highlight-target]")
        ).filter(element =>
            element.dataset.highlightTarget === key
        );

        return candidates.find(isVisible) ||
            candidates[0] ||
            document.getElementById(key);
    }

    function highlightTarget(id, type = "updated") {
        return highlight(findTarget(id), type);
    }

    function onLoad() {
        const requested = Array.from(
            document.querySelectorAll(
                '[data-highlight-on-load="true"]'
            )
        );

        if (requested.length > 0) {
            let target = requested.find(isVisible);

            if (!target) {
                const source = requested[0];
                const key = source.dataset.highlightTarget || "";

                // Match mobile cards to desktop table rows.
                const mobileKey =
                    key.startsWith("question-") ||
                        key.startsWith("category-")
                        ? "mobile-" + key
                        : key;

                target = Array.from(
                    document.querySelectorAll(
                        "[data-highlight-target]"
                    )
                ).find(element =>
                    element.dataset.highlightTarget === mobileKey &&
                    isVisible(element)
                ) || source;
            }

            highlight(
                target,
                requested[0].dataset.highlightType || "updated"
            );

            return;
        }

        // Automatically find validation errors.
        const error = document.querySelector(
            ".input-validation-error, " +
            ".field-validation-error:not(:empty), " +
            ".validation-summary-errors"
        );

        if (error) {
            const field = error.matches(
                "input, textarea, select"
            )
                ? error
                : error.querySelector(
                    "input, textarea, select"
                );

            highlight(field || error, "error");

            if (field) {
                field.focus({ preventScroll: true });
            }
        }
    }

    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", onLoad);
    } else {
        onLoad();
    }

    window.TsebeChangeHighlighter = {
        highlight,
        highlightTarget
    };
})();
