import { useState, type KeyboardEvent, type CSSProperties } from "react";
import { scanApi, assignApi } from "../api/stackerApi";
import { useAuth } from "../context/AuthContext";
import type { BoxView, ShipBoxView } from "../types/stacker";
import { STACKER_PROCESS } from "../config/processConfig";

interface StackerOperationControlsProps {
    onGridViewBoxesLoaded?: (boxes: BoxView[]) => void;
    selectedTargetBox: BoxView | null;
    selectedTargetShipBox: ShipBoxView | null;
    onSelectedTargetBoxChanged: (box: BoxView | null) => void;
    onSelectedTargetShipBoxChanged: (shipBox: ShipBoxView | null) => void;
    onAssignedBoxConfirmed?: (boxNo: string) => void;
}

interface FeedbackState {
    message: string;
    type: "success" | "error" | "idle";
}

function findSuggestedTarget(boxes: BoxView[]) {
    const suggestedBox = boxes.find((box) => box.IsSuggestedTarget);

    if (!suggestedBox) {
        return {
            box: null,
            shipBox: null,
        };
    }

    const suggestedShipBox =
        suggestedBox.ShipBoxes?.find((shipBox) => shipBox.IsSuggestedTarget) ?? null;

    return {
        box: suggestedBox,
        shipBox: suggestedShipBox,
    };
}

export default function StackerOperationControls({
    onGridViewBoxesLoaded,
    selectedTargetBox,
    selectedTargetShipBox,
    onSelectedTargetBoxChanged,
    onSelectedTargetShipBoxChanged,
    onAssignedBoxConfirmed,
}: StackerOperationControlsProps) {
    const { user } = useAuth();
    const [scanValue, setScanValue] = useState("");
    const [scanLoading, setScanLoading] = useState(false);
    const [assignLoading, setAssignLoading] = useState(false);
    const [feedback, setFeedback] = useState<FeedbackState>({
        message: "",
        type: "idle",
    });
    const [assignedBoxMessage, setAssignedBoxMessage] = useState("");

    const canAssign = Boolean(selectedTargetBox && selectedTargetShipBox);

    const showFeedback = (message: string, type: "success" | "error") => {
        setFeedback({ message, type });
        setTimeout(() => setFeedback({ message: "", type: "idle" }), 3500);
    };

    const handleScan = async () => {
        const holder = scanValue.trim();

        if (!holder) return;

        if (!user?.token) {
            showFeedback("Login token is missing. Please sign in again.", "error");
            return;
        }

        setScanLoading(true);

        try {
            const result = await scanApi(holder, user.token);
            const boxes = result.GridViewBoxes ?? [];
            onGridViewBoxesLoaded?.(boxes);

            if (result.Success && result.CanAssign) {
                const suggestedTarget = findSuggestedTarget(boxes);

                if (!suggestedTarget.box) {
                    onSelectedTargetBoxChanged(null);
                    onSelectedTargetShipBoxChanged(null);
                    showFeedback("No suggested Box was found.", "error");
                    return;
                }

                if (!suggestedTarget.shipBox) {
                    onSelectedTargetBoxChanged(null);
                    onSelectedTargetShipBoxChanged(null);
                    showFeedback("No suggested ShipBox was found.", "error");
                    return;
                }

                onSelectedTargetBoxChanged(suggestedTarget.box);
                onSelectedTargetShipBoxChanged(suggestedTarget.shipBox);
                showFeedback("Validation Pass!", "success");
            } else {
                onSelectedTargetBoxChanged(null);
                onSelectedTargetShipBoxChanged(null);
                showFeedback(result.Message || "Validation failed.", "error");
            }
        } catch (err) {
            showFeedback(err instanceof Error ? err.message : "Scan error.", "error");
        } finally {
            setScanLoading(false);
        }
    };

    const handleKeyDown = (e: KeyboardEvent<HTMLInputElement>) => {
        if (e.key === "Enter") {
            e.preventDefault();
            handleScan();
        }
    };

    const handleAssign = async () => {
        const holder = scanValue.trim();

        if (!holder) {
            showFeedback("Holder is required.", "error");
            return;
        }

        if (!user?.token) {
            showFeedback("Login token is missing. Please sign in again.", "error");
            return;
        }

        if (!selectedTargetBox || !selectedTargetShipBox) {
            showFeedback("Please validate first.", "error");
            return;
        }

        const assignedBoxNo = selectedTargetBox.BoxNo;
        const assignedShipBoxName = selectedTargetShipBox.ShipBoxName;

        setAssignLoading(true);

        try {
            const result = await assignApi(
                {
                    Holder: holder,
                    BoxNo: selectedTargetBox.BoxNo,
                    RackNum: selectedTargetBox.RackNum,
                    LayerRowNum: selectedTargetBox.LayerRowNum,
                    LayerColNum: selectedTargetBox.LayerColNum,
                    ShipBoxName: selectedTargetShipBox.ShipBoxName,
                    ShipBoxNum: selectedTargetShipBox.ShipBoxNum,
                    ShipBoxLayerRowNum: selectedTargetShipBox.LayerRowNum,
                    ShipBoxLayerColNum: selectedTargetShipBox.LayerColNum,
                    Process: STACKER_PROCESS,
                },
                user.token
            );

            if (result.GridViewBoxes) {
                onGridViewBoxesLoaded?.(result.GridViewBoxes);
            }

            showFeedback(
                result.Message || (result.Success ? "Assign successful." : "Unable to Assign."),
                result.Success ? "success" : "error"
            );

            if (result.Success) {
                onAssignedBoxConfirmed?.(assignedBoxNo);
                onSelectedTargetBoxChanged(null);
                onSelectedTargetShipBoxChanged(null);
                setAssignedBoxMessage(`Holder was assigned to ${assignedShipBoxName}`);
                setScanValue("");
            }
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
                width: "100%",
                maxWidth: "280px",
                minWidth: 0,
                boxSizing: "border-box",
                alignSelf: "start",
                position: "sticky",
                top: "0",
                zIndex: 5,
                background: "#ffffff",
                border: "1px solid #dde1e9",
                borderRadius: "14px",
                display: "flex",
                flexDirection: "column",
                padding: "1rem 1rem",
                gap: "1.5rem",
            }}
        >
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
                        onSelectedTargetBoxChanged(null);
                        onSelectedTargetShipBoxChanged(null);
                        setAssignedBoxMessage("");
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
                    disabled={assignLoading || !canAssign}
                    style={{
                        background: assignLoading || !canAssign
                            ? "#a0b4d6"
                            : "linear-gradient(90deg, #003d99 0%, #0052cc 100%)",
                        color: "#ffffff",
                        border: "none",
                        borderRadius: "7px",
                        padding: "0.6rem",
                        fontSize: "0.78rem",
                        letterSpacing: "0.1em",
                        textTransform: "uppercase",
                        boxShadow: assignLoading || !canAssign
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

            {assignedBoxMessage && (
                <div
                    className="px-3 py-2"
                    style={{
                        background: "rgba(0,82,204,0.07)",
                        borderLeft: "3px solid #0052cc",
                        color: "#0052cc",
                        borderRadius: "0 7px 7px 0",
                        fontSize: "0.78rem",
                        lineHeight: 1.5,
                    }}
                >
                    {assignedBoxMessage}
                </div>
            )}

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