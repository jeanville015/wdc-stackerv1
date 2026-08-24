import { useEffect, useRef, useState, type KeyboardEvent } from "react";
import { scanApi, assignApi } from "../api/stackerApi";
import { useAuth } from "../context/useAuth";
import type { BoxView, ShipBoxView } from "../types/stacker";
import { formatBoxName, formatRackName, formatShipBoxName, transformValidationMessage } from "../utils/nameTransformers";
import { STACKER_PROCESS } from "../config/processConfig";
import type { CapacityConfig } from "../types/models";

interface StackerOperationControlsProps {
    onGridViewBoxesLoaded?: (boxes: BoxView[]) => void;
    selectedTargetBox: BoxView | null;
    selectedTargetShipBox: ShipBoxView | null;
    onSelectedTargetBoxChanged: (box: BoxView | null) => void;
    onSelectedTargetShipBoxChanged: (shipBox: ShipBoxView | null) => void;
    onAssignedBoxConfirmed?: (boxNo: string) => void;
    boxCount: CapacityConfig["BOX_COUNT"];
    shipBoxBoxCount: CapacityConfig["BOX_COUNT-SHIPBOX"];
}

interface FeedbackState {
    title: string;
    message?: string;
    hint?: string;
    type: "success" | "error";
}

type OperationStage = "scan" | "assignment";

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

                {feedback.message && <span dangerouslySetInnerHTML={{ __html: feedback.message }} />}

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
    boxCount,
    shipBoxBoxCount,
}: StackerOperationControlsProps) { 
    const { user } = useAuth();
    const [scanValue, setScanValue] = useState("");
    const [scanLoading, setScanLoading] = useState(false);
    const [assignLoading, setAssignLoading] = useState(false);
    const [validationFeedback, setValidationFeedback] = useState<FeedbackState | null>(null);
    const [assignmentFeedback, setAssignmentFeedback] = useState<FeedbackState | null>(null);
    const [scannedCamVersion, setScannedCamVersion] = useState<string | null | undefined>(null);

    const [activeStage, setActiveStage] = useState<OperationStage>("scan");
    const scanInputRef = useRef<HTMLInputElement>(null);
    const refocusAfterAssignmentRef = useRef(false);
    useEffect(() => {
        if (assignLoading || !refocusAfterAssignmentRef.current) {
            return;
        }

        refocusAfterAssignmentRef.current = false;
        scanInputRef.current?.focus();
    }, [assignLoading]);

    const canAssign = Boolean(selectedTargetBox && selectedTargetShipBox);

    const getOperationStageClassName = ( stage: OperationStage, feedback: FeedbackState | null ) =>
        [
            "operation-section",
            "operation-stage",
            stage === "assignment"
                ? "operation-assignment-section"
                : "",
            activeStage === stage ? "is-active" : "",
            feedback?.type === "success" ? "is-complete" : "",
            feedback?.type === "error" ? "has-error" : "",
        ]
            .filter(Boolean)
            .join(" ");

    const renderOperationStageMarker = ( stepNumber: 1 | 2, feedback: FeedbackState | null ) => {
        if (feedback?.type === "success") {
            return (
                <i
                    className="fa-solid fa-check"
                    aria-hidden="true"
                />
            );
        }

        if (feedback?.type === "error") {
            return (
                <i
                    className="fa-solid fa-exclamation"
                    aria-hidden="true"
                />
            );
        }

        return <span>{stepNumber}</span>;
    };

    const clearSuggestedTarget = () => {
        onSelectedTargetBoxChanged(null);
        onSelectedTargetShipBoxChanged(null);
    };

    const showValidationFailure = (message: string) => {
        setActiveStage("scan");
        clearSuggestedTarget();
        setValidationFeedback({
            title: "Validation failed",
            message,
            type: "error",
        });
    };

    const showAssignmentFailure = (message: string, hint?: string) => {
        setActiveStage("assignment");
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
        setActiveStage("scan");

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
            if (result.GridViewBoxes && result.GridViewBoxes.length > 0) {
                onGridViewBoxesLoaded?.(result.GridViewBoxes);
            }
            setScannedCamVersion(result.CamVersion);

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

                const targetRackName = formatRackName(suggestedTarget.box.RackNum);
                const targetBoxName = formatBoxName(
                    suggestedTarget.box.LayerRowNum,
                    suggestedTarget.box.LayerColNum,
                    boxCount
                );
                const targetShipBoxName = formatShipBoxName(
                    suggestedTarget.shipBox.LayerRowNum,
                    suggestedTarget.shipBox.LayerColNum,
                    shipBoxBoxCount
                );

                setValidationFeedback({
                    title: "Validation Pass!",
                    message: `Holder will be assigned to<br/>RACK: ${targetRackName},<br/>BLACKBOX: ${targetBoxName},<br/>SHIPBOX: ${targetShipBoxName}`,
                    hint: "Click ASSIGN to continue",
                    type: "success",
                });
                setActiveStage("assignment");
            } else {
                // Holder may already be assigned - locate and highlight its
                // existing box in the rack without enabling assignment.
                const existingBox = boxes.find((box) => box.IsSuggestedTarget) ?? null;
                onSelectedTargetBoxChanged(existingBox);
                onSelectedTargetShipBoxChanged(null);
                setActiveStage("scan");
                setValidationFeedback({
                    title: "Validation failed",
                    message: transformValidationMessage(
                        result.Message || `Holder ${holder} could not be validated.`,
                        boxCount,
                        shipBoxBoxCount
                    ),
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
        const displayShipBoxName = formatShipBoxName(selectedTargetShipBox.LayerRowNum, selectedTargetShipBox.LayerColNum, shipBoxBoxCount);
        const displayBoxName = formatBoxName(selectedTargetBox.LayerRowNum, selectedTargetBox.LayerColNum, boxCount);
        const displayRackName = formatRackName(selectedTargetBox.RackNum);

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
                    CamVersion: scannedCamVersion,
                },
                user.token
            );

            if (!result.Success) {
                showAssignmentFailure(
                    transformValidationMessage(
                        result.Message ||
                        `Unable to assign holder. ${displayShipBoxName} is no longer available.`,
                        boxCount,
                        shipBoxBoxCount
                    ),
                    "Validate again to refresh the target."
                );
                return;
            }

            if (result.GridViewBoxes && result.GridViewBoxes.length > 0) {
                onGridViewBoxesLoaded?.(result.GridViewBoxes);
            }

            setValidationFeedback(null);
            setAssignmentFeedback({
                title: "Assignment complete",
                message: `Holder was assigned to:<br/> RACK: ${displayRackName},<br/>BLACKBOX: ${displayBoxName},<br/>SHIPBOX: ${displayShipBoxName}.`,
                type: "success",
            });

            setActiveStage("scan");
            refocusAfterAssignmentRef.current = true;

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

    const validationHasError = validationFeedback?.type === "error";

    return (
        <aside
            className="stacker-operation-controls"
            aria-label="Scanning and assignment controls"
        >
            <h2 className="operation-panel-title">Scanning / Assignment</h2>

            <section className={getOperationStageClassName("scan", validationFeedback)} >

                <div className="operation-stage-marker" aria-hidden="true">
                    {renderOperationStageMarker(1, validationFeedback)}
                </div>

                <div className="operation-stage-content">

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
                            ref={scanInputRef}
                            id="scan-input"
                            type="text"
                            className="form-control"
                            placeholder="Scan Holder No..."
                            value={scanValue}
                            autoFocus
                            autoComplete="off"
                            onFocus={() => setActiveStage("scan")}
                            aria-invalid={validationHasError}
                            aria-describedby={
                                validationFeedback ? "validation-feedback" : undefined
                            }
                            onChange={(e) => {
                                setActiveStage("scan");
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
                </div>
            </section>

            <section className={getOperationStageClassName("assignment", assignmentFeedback)} >
                <div className="operation-stage-marker" aria-hidden="true">
                    {renderOperationStageMarker(2, assignmentFeedback)}
                </div>

                <div className="operation-stage-content">
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
                </div>
            </section>
        </aside>
    );
}
