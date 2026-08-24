import { useEffect, useRef } from "react";
import type { HoldCheckResult } from "../../api/holdCheckApi";

interface HoldCheckResultModalProps {
    result: HoldCheckResult | null;
    onClose: () => void;
}

export default function HoldCheckResultModal({
    result,
    onClose,
}: HoldCheckResultModalProps) {
    const closeButtonRef = useRef<HTMLButtonElement>(null);

    useEffect(() => {
        if (!result) return;

        const previouslyFocusedElement =
            document.activeElement instanceof HTMLElement
                ? document.activeElement
                : null;
        const previousBodyOverflow = document.body.style.overflow;

        document.body.style.overflow = "hidden";

        const focusFrame = window.requestAnimationFrame(() => {
            closeButtonRef.current?.focus();
        });

        const handleKeyDown = (event: KeyboardEvent) => {
            if (event.key === "Escape") {
                onClose();
            }
        };

        document.addEventListener("keydown", handleKeyDown);

        return () => {
            window.cancelAnimationFrame(focusFrame);
            document.removeEventListener("keydown", handleKeyDown);
            document.body.style.overflow = previousBodyOverflow;
            previouslyFocusedElement?.focus();
        };
    }, [result, onClose]);

    if (!result) return null;

    const isSuccess = result.success;
    const data = result.data;

    const summaryItems = data
        ? [
            {
                label: "Set to hold",
                value: data.holdersSetToHold,
                positive: false,
            },
            {
                label: "Cleared",
                value: data.holdersCleared,
                positive: true,
            },
            {
                label: "Already on hold",
                value: data.holdersAlreadyOnHold,
                positive: false,
            },
            {
                label: "Already clear",
                value: data.holdersAlreadyClear,
                positive: true,
            },
        ]
        : [];

    return (
        <div
            className="hold-check-result-backdrop"
            onMouseDown={(event) => {
                if (event.target === event.currentTarget) {
                    onClose();
                }
            }}
        >
            <section
                className={[
                    "hold-check-result-modal",
                    !isSuccess && "is-error",
                ]
                    .filter(Boolean)
                    .join(" ")}
                role="dialog"
                aria-modal="true"
                aria-labelledby="hold-check-result-title"
                aria-describedby="hold-check-result-description"
            >
                <header className="hold-check-result-header">
                    <span
                        className="hold-check-result-icon"
                        aria-hidden="true"
                    >
                        <i
                            className={
                                isSuccess
                                    ? "fa-solid fa-check"
                                    : "fa-solid fa-triangle-exclamation"
                            }
                        />
                    </span>

                    <div className="hold-check-result-heading">
                        <h2 id="hold-check-result-title">
                            {isSuccess
                                ? "Hold check completed"
                                : "Hold check failed"}
                        </h2>

                        <p id="hold-check-result-description">
                            {isSuccess
                                ? "Rack status has been refreshed with the latest hold results."
                                : result.message ||
                                "The hold check could not be completed."}
                        </p>
                    </div>

                    <button
                        type="button"
                        className="hold-check-result-x"
                        onClick={onClose}
                        aria-label="Close hold check result"
                    >
                        <i
                            className="fa-solid fa-xmark"
                            aria-hidden="true"
                        />
                    </button>
                </header>

                <div className="hold-check-result-body">
                    {isSuccess && data ? (
                        <>
                            <div className="hold-check-result-total">
                                <strong>{data.totalHolders}</strong>
                                <span>Total holders</span>
                            </div>

                            <div className="hold-check-result-summary">
                                {summaryItems.map((item) => (
                                    <div
                                        className="hold-check-result-row"
                                        key={item.label}
                                    >
                                        <span>{item.label}</span>

                                        <strong
                                            className={
                                                item.positive
                                                    ? "is-positive"
                                                    : undefined
                                            }
                                        >
                                            {item.value}
                                        </strong>
                                    </div>
                                ))}
                            </div>
                        </>
                    ) : (
                        <div className="hold-check-result-error">
                            <p>
                                Please check the hold-check service connection
                                and try again.
                            </p>

                            {result.error && (
                                <small>{result.error}</small>
                            )}
                        </div>
                    )}
                </div>

                <footer className="hold-check-result-footer">
                    <button
                        ref={closeButtonRef}
                        type="button"
                        className="hold-check-result-close"
                        onClick={onClose}
                    >
                        Close
                    </button>
                </footer>
            </section>
        </div>
    );
}