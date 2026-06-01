import { useState, type KeyboardEvent, type CSSProperties } from "react";
import { scanApi, assignApi } from "../api/stackerApi";
import { useAuth } from "../context/AuthContext";

interface FeedbackState {
    message: string;
    type: "success" | "error" | "idle";
}

export default function LeftNav() {
    const { user } = useAuth();

    const [scanValue, setScanValue] = useState("");
    const [scanLoading, setScanLoading] = useState(false);
    const [assignLoading, setAssignLoading] = useState(false);
    const [assignEnabled, setAssignEnabled] = useState(false);
    const [feedback, setFeedback] = useState<FeedbackState>({
        message: "",
        type: "idle",
    });

    const showFeedback = (message: string, type: "success" | "error") => {
        setFeedback({ message, type });
        setTimeout(() => setFeedback({ message: "", type: "idle" }), 3500);
    };

    const handleScan = async () => {
        const holder = scanValue.trim();

        if (!holder) return;

        if (!user?.token) {
            setAssignEnabled(false);
            showFeedback("Login token is missing. Please sign in again.", "error");
            return;
        }

        setScanLoading(true);
        setAssignEnabled(false);

        try {
            const result = await scanApi(holder, user.token);

            if (result.Success && result.CanAssign) {
                window.alert("Validation Pass!");
                setAssignEnabled(true);
                showFeedback(result.Message, "success");
            } else {
                setAssignEnabled(false);
                showFeedback(result.Message || "Validation failed.", "error");
            }
        } catch (err) {
            setAssignEnabled(false);
            showFeedback(
                err instanceof Error ? err.message : "Scan error.",
                "error"
            );
        } finally {
            setScanLoading(false);
        }
    };

    const handleKeyDown = (e: KeyboardEvent<HTMLInputElement>) => {
        if (e.key === "Enter") handleScan();
    };

    const handleAssign = async () => {
        setAssignLoading(true);
        try {
            const result = await assignApi();
            showFeedback(
                result.success ? result.message : result.message,
                result.success ? "success" : "error"
            );
        } catch (err) {
            showFeedback(
                err instanceof Error ? err.message : "Assign error.",
                "error"
            );
        } finally {
            setAssignLoading(false);
        }
    };

    const feedbackStyle: CSSProperties =
        feedback.type === "success"
            ? {
                background: "rgba(0,82,204,0.07)",
                borderLeft: "3px solid #0052cc",
                color: "#0052cc",
            }
            : feedback.type === "error"
                ? {
                    background: "rgba(210,50,50,0.07)",
                    borderLeft: "3px solid #d23232",
                    color: "#d23232",
                }
                : {};

    return (
        <aside
            style={{
                width: "280px",
                minWidth: "280px",
                background: "#ffffff",
                borderRight: "1px solid #dde1e9",
                display: "flex",
                flexDirection: "column",
                padding: "1.75rem 1.25rem",
                gap: "1.5rem",
            }}
        >
            <p
                className="text-uppercase fw-semibold mb-0"
                style={{ fontSize: "0.62rem", letterSpacing: "0.25em", color: "#00c5ff" }}
            />

            <div
                style={{
                    background: "#f4f5f7",
                    border: "1px solid #dde1e9",
                    borderRadius: "10px",
                    padding: "1rem",
                    display: "flex",
                    flexDirection: "column",
                    gap: "0.75rem",
                }}
            >
                <label
                    htmlFor="scan-input"
                    className="fw-semibold mb-0"
                    style={{
                        fontSize: "0.72rem",
                        letterSpacing: "0.08em",
                        textTransform: "uppercase",
                        color: "#172b4d",
                    }}
                />

                <input
                    id="scan-input"
                    type="text"
                    className="form-control"
                    placeholder="Scan Holder Number..."
                    value={scanValue}
                    onChange={(e) => {
                        setScanValue(e.target.value);
                        setAssignEnabled(false);
                    }}
                    onKeyDown={handleKeyDown}
                    disabled={scanLoading}
                    style={{
                        border: "1.5px solid #dde1e9",
                        borderRadius: "7px",
                        color: "#172b4d",
                        fontSize: "0.88rem",
                        padding: "0.55rem 0.8rem",
                        background: "#ffffff",
                    }}
                />

                <button
                    className="btn w-100 fw-bold"
                    onClick={handleScan}
                    disabled={scanLoading || !scanValue.trim()}
                    style={{
                        background: scanLoading || !scanValue.trim()
                            ? "#a0b4d6"
                            : "linear-gradient(90deg, #0052cc 0%, #0065ff 100%)",
                        color: "#ffffff",
                        border: "none",
                        borderRadius: "7px",
                        padding: "0.55rem",
                        fontSize: "0.78rem",
                        letterSpacing: "0.1em",
                        textTransform: "uppercase",
                        boxShadow: scanLoading || !scanValue.trim()
                            ? "none"
                            : "0 3px 10px rgba(0,82,204,0.25)",
                        transition: "all 0.15s",
                    }}
                >
                    {scanLoading ? (
                        <>
                            <span
                                className="spinner-border spinner-border-sm me-2"
                                role="status"
                                aria-hidden="true"
                            />
                            validating...
                        </>
                    ) : (
                        "Validate"
                    )}
                </button>
            </div>

            <div>
                <p
                    className="fw-semibold mb-2"
                    style={{
                        fontSize: "0.72rem",
                        letterSpacing: "0.08em",
                        textTransform: "uppercase",
                        color: "#172b4d",
                    }}
                >
                    Assignment
                </p>

                <button
                    className="btn w-100 fw-bold"
                    onClick={handleAssign}
                    disabled={assignLoading || !assignEnabled}
                    style={{
                        background: assignLoading || !assignEnabled
                            ? "#a0b4d6"
                            : "linear-gradient(90deg, #003d99 0%, #0052cc 100%)",
                        color: "#ffffff",
                        border: "none",
                        borderRadius: "7px",
                        padding: "0.6rem",
                        fontSize: "0.78rem",
                        letterSpacing: "0.1em",
                        textTransform: "uppercase",
                        boxShadow: assignLoading || !assignEnabled
                            ? "none"
                            : "0 3px 10px rgba(0,61,153,0.25)",
                        transition: "all 0.15s",
                    }}
                >
                    {assignLoading ? (
                        <>
                            <span
                                className="spinner-border spinner-border-sm me-2"
                                role="status"
                                aria-hidden="true"
                            />
                            Assigning...
                        </>
                    ) : (
                        "Assign"
                    )}
                </button>
            </div>

            {feedback.type !== "idle" && (
                <div
                    className="px-3 py-2"
                    style={{
                        ...feedbackStyle,
                        borderRadius: "0 7px 7px 0",
                        fontSize: "0.78rem",
                        lineHeight: 1.5,
                    }}
                >
                    {feedback.message}
                </div>
            )}
        </aside>
    );
}