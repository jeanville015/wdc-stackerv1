import { useState, type KeyboardEvent } from "react";
import { scanApi, assignApi, exportCsvApi } from "../api/stackerApi";
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
    title: string;
    message?: string;
    hint?: string;
    type: "success" | "error";
}

interface OperationNoticeProps {
    id: string;
    feedback: FeedbackState;
}

function OperationNotice({ id, feedback }: OperationNoticeProps) {
    const isError = feedback.type === "error";

    return (
        <div
            id={id}
            className={`operation-feedback is-${feedback.type}`}
            role={isError ? "alert" : "status"}
            aria-live={isError ? "assertive" : "polite"}
            aria-atomic="true"
        >
            <i
                className={
                    isError
                        ? "fa-solid fa-circle-exclamation"
                        : "fa-solid fa-circle-check"
                }
                aria-hidden="true"
            />

            <div>
                <strong>{feedback.title}</strong>

                {feedback.message && <span>{feedback.message}</span>}

                {feedback.hint && (
                    <span className="operation-feedback-hint">
                        {feedback.hint}
                    </span>
                )}
            </div>
        </div>
    );
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
    const [csvExportLoading, setCsvExportLoading] = useState(false);
    const [validationFeedback, setValidationFeedback] =
        useState<FeedbackState | null>(null);
    const [assignmentFeedback, setAssignmentFeedback] =
        useState<FeedbackState | null>(null);

    const canAssign = Boolean(selectedTargetBox && selectedTargetShipBox);

    const clearSuggestedTarget = () => {
        onSelectedTargetBoxChanged(null);
        onSelectedTargetShipBoxChanged(null);
    };

    const showValidationFailure = (message: string) => {
        clearSuggestedTarget();
        setValidationFeedback({
            title: "Validation failed",
            message,
            type: "error",
        });
    };

    const showAssignmentFailure = (message: string, hint?: string) => {
        setAssignmentFeedback({
            title: "Assignment failed",
            message,
            hint,
            type: "error",
        });
    };

    const validateDisabled =
        scanLoading || assignLoading || !scanValue.trim();
    const assignDisabled = scanLoading || assignLoading || !canAssign;

    const handleScan = async () => {
        const holder = scanValue.trim();

        setValidationFeedback(null);
        setAssignmentFeedback(null);
        clearSuggestedTarget();

        if (!holder) {
            showValidationFailure("Holder is required.");
            return;
        }

        if (!user?.token) {
            showValidationFailure("Login token is missing. Please sign in again.");
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
                    showValidationFailure("No suggested Box was found.");
                    return;
                }

                if (!suggestedTarget.shipBox) {
                    showValidationFailure("No suggested ShipBox was found.");
                    return;
                }

                onSelectedTargetBoxChanged(suggestedTarget.box);
                onSelectedTargetShipBoxChanged(suggestedTarget.shipBox);
                setValidationFeedback({
                    title: "Validation Pass!",
                    type: "success",
                });
            } else {
                // Holder may already be assigned - locate and highlight its
                // existing box in the rack without enabling assignment.
                const existingBox = boxes.find((box) => box.IsSuggestedTarget) ?? null;
                onSelectedTargetBoxChanged(existingBox);
                onSelectedTargetShipBoxChanged(null);
                setValidationFeedback({
                    title: "Validation failed",
                    message: result.Message || `Holder ${holder} could not be validated.`,
                    type: "error",
                });
            }
        } catch (err) {
            showValidationFailure(
                err instanceof Error ? err.message : "Unable to validate the holder."
            );
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

        setAssignmentFeedback(null);

        if (!holder) {
            showAssignmentFailure("Holder is required.");
            return;
        }

        if (!user?.token) {
            showAssignmentFailure("Login token is missing. Please sign in again.");
            return;
        }

        if (!selectedTargetBox || !selectedTargetShipBox) {
            showAssignmentFailure("Please validate the holder first.");
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

            if (!result.Success) {
                showAssignmentFailure(
                    result.Message ||
                        `Unable to assign holder. ${assignedShipBoxName} is no longer available.`,
                    "Validate again to refresh the target."
                );
                return;
            }

            if (result.GridViewBoxes) {
                onGridViewBoxesLoaded?.(result.GridViewBoxes);
            }

            setValidationFeedback(null);
            setAssignmentFeedback({
                title: "Assignment complete",
                message: `Holder was assigned to ${assignedShipBoxName}.`,
                type: "success",
            });

            onAssignedBoxConfirmed?.(assignedBoxNo);
            clearSuggestedTarget();
            setScanValue("");
        } catch (err) {
            showAssignmentFailure(
                err instanceof Error ? err.message : "Unable to assign the holder.",
                "Validate again to refresh the target."
            );
        } finally {
            setAssignLoading(false);
        }
    };

    const handleExportCsv = async () => {
        if (!user?.token) {
            showAssignmentFailure("Login token is missing. Please sign in again.");
            return;
        }

        setCsvExportLoading(true);
        try {
            await exportCsvApi(user.token);
        } catch (err) {
            showAssignmentFailure(
                err instanceof Error ? err.message : "CSV export failed."
            );
        } finally {
            setCsvExportLoading(false);
        }
    };

    const validationHasError = validationFeedback?.type === "error";

    return (
        <aside
            className="stacker-operation-controls"
            aria-label="Scanning and assignment controls"
        >
            <h2 className="operation-panel-title">Scanning / Assignment</h2>

            <section className="operation-section">
                <label htmlFor="scan-input" className="operation-field-label">
                    Scan Holder
                </label>

                <div
                    className={[
                        "operation-scan-field",
                        validationHasError ? "is-error" : "",
                    ]
                        .filter(Boolean)
                        .join(" ")}
                >
                    <input
                        id="scan-input"
                        type="text"
                        className="form-control"
                        placeholder="Scan Holder Number..."
                        value={scanValue}
                        autoComplete="off"
                        aria-invalid={validationHasError}
                        aria-describedby={
                            validationFeedback ? "validation-feedback" : undefined
                        }
                        onChange={(e) => {
                            setScanValue(e.target.value);
                            clearSuggestedTarget();
                            setValidationFeedback(null);
                            setAssignmentFeedback(null);
                        }}
                        onKeyDown={handleKeyDown}
                        disabled={scanLoading || assignLoading}
                    />

                    <span className="operation-barcode-icon" aria-hidden="true">
                        <i className="fa-solid fa-barcode" />
                    </span>
                </div>

                <button
                    type="button"
                    className="btn operation-primary-button"
                    onClick={handleScan}
                    disabled={validateDisabled}
                >
                    {scanLoading ? (
                        <>
                            <span
                                className="spinner-border spinner-border-sm"
                                aria-hidden="true"
                            />
                            Validating...
                        </>
                    ) : (
                        "Validate"
                    )}
                </button>

                {validationFeedback && (
                    <OperationNotice
                        id="validation-feedback"
                        feedback={validationFeedback}
                    />
                )}
            </section>

            <section className="operation-section operation-assignment-section">
                <h3 className="operation-section-title">Assignment</h3>

                <button
                    type="button"
                    className="btn operation-primary-button"
                    onClick={handleAssign}
                    disabled={assignDisabled}
                >
                    {assignLoading ? (
                        <>
                            <span
                                className="spinner-border spinner-border-sm"
                                aria-hidden="true"
                            />
                            Assigning...
                        </>
                    ) : (
                        "Assign"
                    )}
                </button>

                {assignmentFeedback ? (
                    <OperationNotice
                        id="assignment-feedback"
                        feedback={assignmentFeedback}
                    />
                ) : (
                    <p className="operation-helper-text">
                        Assign the validated holder to the recommended destination.
                    </p>
                )}
            </section>

            <section className="operation-section operation-assignment-section">
                <h3 className="operation-section-title">Export</h3>

                <button
                    type="button"
                    className="btn operation-secondary-button"
                    onClick={handleExportCsv}
                    disabled={csvExportLoading}
                >
                    {csvExportLoading ? (
                        <>
                            <span
                                className="spinner-border spinner-border-sm"
                                aria-hidden="true"
                            />
                            Exporting...
                        </>
                    ) : (
                        <>
                            <i className="fa-solid fa-file-csv" aria-hidden="true" />
                            Download CSV
                        </>
                    )}
                </button>
            </section>
        </aside>
    );
}
