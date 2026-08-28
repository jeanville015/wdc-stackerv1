import {
    useEffect,
    useRef,
    useState,
    type KeyboardEvent,
} from "react";
import { scanApi, assignApi } from "../api/stackerApi";
import { useAuth } from "../context/useAuth";
import type { BoxView } from "../types/stacker";
import {
    formatBoxName,
    formatRackName,
} from "../utils/nameTransformers";
import { STACKER_PROCESS } from "../config/processConfig";

interface StackerOperationControlsProps {
    onGridViewBoxesLoaded?: (boxes: BoxView[]) => void;
    onAssignedBoxConfirmed?: (boxNo: string) => void;
    onSelectedTargetBoxChanged?: (
        box: BoxView | null,
        isExistingLocation?: boolean
    ) => void;
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

function OperationNotice({
    id,
    feedback,
}: OperationNoticeProps) {
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

                {feedback.message && (
                    <span style={{ whiteSpace: "pre-line" }}>
                        {feedback.message}
                    </span>
                )}

                {feedback.hint && (
                    <span className="operation-feedback-hint">
                        {feedback.hint}
                    </span>
                )}
            </div>
        </div>
    );
}

export default function StackerOperationControls({
    onGridViewBoxesLoaded,
    onAssignedBoxConfirmed,
    onSelectedTargetBoxChanged,
}: StackerOperationControlsProps) {
    const { user } = useAuth();

    const [scanValue, setScanValue] = useState("");
    const [scanLoading, setScanLoading] = useState(false);
    const [assignLoading, setAssignLoading] = useState(false);

    const [suggestedTargetBox, setSuggestedTargetBox] =
        useState<BoxView | null>(null);

    const [validationFeedback, setValidationFeedback] =
        useState<FeedbackState | null>(null);

    const [assignmentFeedback, setAssignmentFeedback] =
        useState<FeedbackState | null>(null);

    const [activeStage, setActiveStage] =
        useState<OperationStage>("scan");

    const scanInputRef = useRef<HTMLInputElement>(null);
    const refocusAfterAssignmentRef = useRef(false);

    useEffect(() => {
        if (
            assignLoading ||
            !refocusAfterAssignmentRef.current
        ) {
            return;
        }

        refocusAfterAssignmentRef.current = false;
        scanInputRef.current?.focus();
    }, [assignLoading]);

    const canAssign = Boolean(suggestedTargetBox);

    const getOperationStageClassName = (
        stage: OperationStage,
        feedback: FeedbackState | null
    ) =>
        [
            "operation-section",
            "operation-stage",
            stage === "assignment"
                ? "operation-assignment-section"
                : "",
            activeStage === stage ? "is-active" : "",
            feedback?.type === "success"
                ? "is-complete"
                : "",
            feedback?.type === "error"
                ? "has-error"
                : "",
        ]
            .filter(Boolean)
            .join(" ");

    const renderOperationStageMarker = (
        stepNumber: 1 | 2,
        feedback: FeedbackState | null
    ) => {
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
        setSuggestedTargetBox(null);
        onSelectedTargetBoxChanged?.(null, false);
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

    const showAssignmentFailure = (
        message: string,
        hint?: string
    ) => {
        setActiveStage("assignment");

        setAssignmentFeedback({
            title: "Assignment failed",
            message,
            hint,
            type: "error",
        });
    };

    const validateDisabled =
        scanLoading ||
        assignLoading ||
        !scanValue.trim();

    const assignDisabled =
        scanLoading ||
        assignLoading ||
        !canAssign;

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
            showValidationFailure(
                "Login token is missing. Please sign in again."
            );
            return;
        }

        setScanLoading(true);

        try {
            const result = await scanApi(
                holder,
                user.token
            );

            const boxes = result.GridViewBoxes ?? [];

            onGridViewBoxesLoaded?.(boxes);

            const suggestedTarget =
                boxes.find(
                    (box) => box.IsSuggestedTarget
                ) ?? null;

            if (
                result.Success &&
                result.CanAssign &&
                suggestedTarget
            ) {
                setSuggestedTargetBox(suggestedTarget);

                onSelectedTargetBoxChanged?.(
                    suggestedTarget,
                    false
                );

                setValidationFeedback({
                    title: "Validation Pass!",
                    message:
                        `Holder will be assigned to:\n` +
                        `RACK: ${formatRackName(
                            suggestedTarget.RackNum
                        )}\n` +
                        `BOX: ${formatBoxName(
                            suggestedTarget.BoxNo,
                            suggestedTarget.RackNum
                        )}`,
                    hint: "Click ASSIGN to continue",
                    type: "success",
                });

                setActiveStage("assignment");
                return;
            }

            /*
             * When the holder is already assigned, the API may
             * return its current box as IsSuggestedTarget.
             * Highlight that location without enabling Assign.
             */
            const existingBox =
                boxes.find(
                    (box) => box.IsSuggestedTarget
                ) ?? null;

            setSuggestedTargetBox(null);

            onSelectedTargetBoxChanged?.(
                existingBox,
                true
            );

            setActiveStage("scan");

            setValidationFeedback({
                title: "Validation failed",
                message:
                    result.Message ||
                    `Holder ${holder} could not be validated.`,
                type: "error",
            });
        } catch (err) {
            showValidationFailure(
                err instanceof Error
                    ? err.message
                    : "Unable to validate the holder."
            );
        } finally {
            setScanLoading(false);
        }
    };

    const handleKeyDown = (
        event: KeyboardEvent<HTMLInputElement>
    ) => {
        if (event.key === "Enter") {
            event.preventDefault();
            void handleScan();
        }
    };

    const handleAssign = async () => {
        const holder = scanValue.trim();

        setActiveStage("assignment");
        setAssignmentFeedback(null);

        if (!holder) {
            showAssignmentFailure(
                "Holder is required."
            );
            return;
        }

        if (!user?.token) {
            showAssignmentFailure(
                "Login token is missing. Please sign in again."
            );
            return;
        }

        if (!suggestedTargetBox) {
            showAssignmentFailure(
                "Please validate the holder first."
            );
            return;
        }

        const assignedBoxNo =
            suggestedTargetBox.BoxNo;

        const displayRackName =
            formatRackName(
                suggestedTargetBox.RackNum
            );

        const displayBoxName =
            formatBoxName(
                suggestedTargetBox.BoxNo,
                suggestedTargetBox.RackNum
            );

        setAssignLoading(true);

        try {
            const result = await assignApi(
                {
                    Holder: holder,
                    BoxNo: suggestedTargetBox.BoxNo,
                    RackNum:
                        suggestedTargetBox.RackNum,
                    LayerRowNum:
                        suggestedTargetBox.LayerRowNum,
                    LayerColNum:
                        suggestedTargetBox.LayerColNum,
                    Process: STACKER_PROCESS,
                },
                user.token
            );

            if (!result.Success) {
                showAssignmentFailure(
                    result.Message ||
                    "Unable to assign the holder.",
                    "Validate again to refresh the target."
                );
                return;
            }

            if (result.GridViewBoxes) {
                onGridViewBoxesLoaded?.(
                    result.GridViewBoxes
                );
            }

            setValidationFeedback(null);

            setAssignmentFeedback({
                title: "Assignment complete",
                message:
                    `Holder was assigned to:\n` +
                    `RACK: ${displayRackName}\n` +
                    `BOX: ${displayBoxName}`,
                type: "success",
            });

            /*
             * Assignment is complete, so make the Scan stage
             * active again and return focus to the scan input.
             */
            setActiveStage("scan");
            refocusAfterAssignmentRef.current = true;

            onAssignedBoxConfirmed?.(
                assignedBoxNo
            );

            setScanValue("");
            clearSuggestedTarget();
        } catch (err) {
            showAssignmentFailure(
                err instanceof Error
                    ? err.message
                    : "Unable to assign the holder.",
                "Validate again to refresh the target."
            );
        } finally {
            setAssignLoading(false);
        }
    };

    const validationHasError =
        validationFeedback?.type === "error";

    return (
        <aside
            className="stacker-operation-controls"
            aria-label="Scanning and assignment controls"
        >
            <h2 className="operation-panel-title">
                Scanning / Assignment
            </h2>

            <section
                className={getOperationStageClassName(
                    "scan",
                    validationFeedback
                )}
            >
                <div
                    className="operation-stage-marker"
                    aria-hidden="true"
                >
                    {renderOperationStageMarker(
                        1,
                        validationFeedback
                    )}
                </div>

                <div className="operation-stage-content">
                    <label
                        htmlFor="scan-input"
                        className="operation-field-label"
                    >
                        Scan Holder
                    </label>

                    <div
                        className={[
                            "operation-scan-field",
                            validationHasError
                                ? "is-error"
                                : "",
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
                            onFocus={() =>
                                setActiveStage("scan")
                            }
                            aria-invalid={
                                validationHasError
                            }
                            aria-describedby={
                                validationFeedback
                                    ? "validation-feedback"
                                    : undefined
                            }
                            onChange={(event) => {
                                setActiveStage("scan");
                                setScanValue(
                                    event.target.value
                                );
                                clearSuggestedTarget();
                                setValidationFeedback(
                                    null
                                );
                                setAssignmentFeedback(
                                    null
                                );
                            }}
                            onKeyDown={handleKeyDown}
                            disabled={
                                scanLoading ||
                                assignLoading
                            }
                        />

                        <span
                            className="operation-barcode-icon"
                            aria-hidden="true"
                        >
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
                            feedback={
                                validationFeedback
                            }
                        />
                    )}
                </div>
            </section>

            <section
                className={getOperationStageClassName(
                    "assignment",
                    assignmentFeedback
                )}
            >
                <div
                    className="operation-stage-marker"
                    aria-hidden="true"
                >
                    {renderOperationStageMarker(
                        2,
                        assignmentFeedback
                    )}
                </div>

                <div className="operation-stage-content">
                    <h3 className="operation-section-title">
                        Assignment
                    </h3>

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
                            feedback={
                                assignmentFeedback
                            }
                        />
                    ) : (
                        <p className="operation-helper-text">
                            Assign the validated holder to
                            the recommended destination.
                        </p>
                    )}
                </div>
            </section>
        </aside>
    );
}