import {
    useEffect,
    useRef,
    useState,
    type FormEvent,
    type MouseEvent,
} from "react";
import type {
    FgiWithdrawalDisassociationPreview,
    FgiWithdrawalRequest,
    FgiWithdrawalSourceRecord,
} from "../../types/withdrawal";

interface Props {
    request: FgiWithdrawalRequest;
    preview: FgiWithdrawalDisassociationPreview;
    onClose: () => void;
    onDisassociate: (
        shippingId: string,
        includedHolders: string[]
    ) => Promise<void>;
}

interface RecordsTableProps {
    title: string;
    tone: "included" | "skipped";
    records: FgiWithdrawalSourceRecord[];
    maximumTotalQty: number;
    targetTotal: number;
    emptyMessage: string;
    focusedRecordIndex?: number | null;
}

function displayValue(
    value: string | number | null | undefined
): string | number {
    return value === null ||
        value === undefined ||
        value === ""
        ? "—"
        : value;
}

function formatShortDate(value: string | null): string {
    if (!value) {
        return "—";
    }

    const dateOnly = value.split("T")[0];
    const [year, month, day] = dateOnly.split("-");

    return year && month && day
        ? `${month}/${day}/${year}`
        : value;
}

function formatTimestamp(value: string | null): string {
    if (!value) {
        return "—";
    }

    return value
        .replace("T", " ")
        .replace(/Z$/, "");
}

function RecordsTable({
    title,
    tone,
    records,
    maximumTotalQty,
    emptyMessage,
    focusedRecordIndex = null,
    targetTotal,
}: RecordsTableProps) {
    const scrollContainerRef =
        useRef<HTMLDivElement | null>(null);

    const focusedRowRef =
        useRef<HTMLTableRowElement | null>(null);

    const hasVerticalScroll =
        tone === "included" &&
        records.length > 4;

    useEffect(() => {
        const container =
            scrollContainerRef.current;

        const row =
            focusedRowRef.current;

        if (
            focusedRecordIndex === null ||
            !container ||
            !row
        ) {
            return;
        }

        const rowCenter =
            row.offsetTop +
            row.offsetHeight / 2;

        const targetScrollTop =
            rowCenter -
            container.clientHeight / 2;

        container.scrollTo({
            top: Math.max(0, targetScrollTop),
            behavior: "smooth",
        });

        row.focus({
            preventScroll: true,
        });
    }, [focusedRecordIndex]);
    return (
        <div className="withdrawal-fifo-group">
            <h4
                className={`withdrawal-fifo-group-title is-${tone}`}
            >
                {title}
            </h4>

            <div ref={scrollContainerRef} className={`table-responsive withdrawal-fifo-table-scroll ${hasVerticalScroll ? "is-scrollable" : "" }`}>
                <table className="table align-middle mb-0 withdrawal-fifo-table">
                    <thead>
                        <tr>
                            <th scope="col">HOLDERS</th>
                            <th scope="col">DATE &amp; TIME</th>
                            <th scope="col">QTY</th>
                            <th scope="col">
                                CUMULATIVE QTY
                            </th>
                            <th scope="col">NOTE</th>
                            <th scope="col">STATUS</th>
                        </tr>
                    </thead>

                    <tbody>
                        {records.map((record, index) => {
                            const isVerified = record.Status .trim() .toUpperCase() === "VERIFIED"; 
                            const isFocusedRecord = focusedRecordIndex === index;

                            return (
                            <tr
                                key={[
                                    record.Holder,
                                    record.UpdateTs ?? "",
                                    index,
                                ].join("-")}
                                ref={
                                    isFocusedRecord
                                        ? focusedRowRef
                                        : null
                                }
                                className={[
                                    isVerified
                                        ? "is-verified"
                                        : "",
                                    isFocusedRecord
                                        ? "is-verification-focus"
                                        : "",
                                ]
                                    .filter(Boolean)
                                    .join(" ")}
                                tabIndex={
                                    isFocusedRecord
                                        ? -1
                                        : undefined
                                }
                            >
                                <td>{record.Holder}</td>
                                <td>
                                    {formatTimestamp(
                                        record.UpdateTs
                                    )}
                                </td>
                                <td>{record.Qty}</td>
                                <td>
                                    {record.IsIncluded
                                        ? record.RunningTotal
                                        : "—"}
                                </td>
                                <td
                                    className={
                                        record.IsIncluded
                                            ? ""
                                            : "withdrawal-fifo-skipped-note"
                                    }
                                >
                                        {record.IsIncluded
                                            ? "—"
                                            : record.Status === "IN-SITE HOLD" ||
                                                record.Status === "AHS HOLD"
                                                ? record.Status
                                                : record.RunningTotal >=
                                                    targetTotal
                                                    ? "TARGET TOTAL ALREADY REACHED"
                                                    : `EXCEEDS FIFO CAP ${maximumTotalQty}`}
                                </td>

                                <td aria-label="Status">
                                    {record.Status && (
                                        <span
                                            className={`withdrawal-fifo-status ${record.Status === "VERIFIED"
                                                ? "is-verified"
                                                : record.Status === "IN-SITE HOLD" ||
                                                    record.Status === "AHS HOLD"
                                                    ? "is-insite-hold"
                                                    : "is-hold-pass"
                                            }`}
                                        >
                                            {record.Status}
                                        </span>
                                    )}
                                </td>
                                </tr>
                            );
                        })}

                        {records.length === 0 && (
                            <tr>
                                <td
                                    colSpan={6}
                                    className="text-center text-muted p-3"
                                >
                                    {emptyMessage}
                                </td>
                            </tr>
                        )}
                    </tbody>
                </table>
            </div>
        </div>
    );
}

export default function WithdrawalDisassociationModal({
    request,
    preview,
    onClose,
    onDisassociate,
}: Props) {
    const [shippingId, setShippingId] = useState("");
    const [holder, setHolder] = useState("");
    const [holderNotFound, setHolderNotFound] = useState(false);
    const [
        focusedIncludedRecordIndex,
        setFocusedIncludedRecordIndex,
    ] = useState<number | null>(null);

    const [sourceRecords, setSourceRecords] =
        useState<FgiWithdrawalSourceRecord[]>(() =>
            preview.SourceRecords.map((record) => ({
                ...record,
            }))
        );

    const [
        confirmationOpen,
        setConfirmationOpen,
    ] = useState(false);

    const [
        isDisassociating,
        setIsDisassociating,
    ] = useState(false);

    const [
        disassociateSubmitError,
        setDisassociateSubmitError,
    ] = useState("");

    const confirmationCancelRef =
        useRef<HTMLButtonElement | null>(null);

    useEffect(() => {
        if (confirmationOpen) {
            confirmationCancelRef.current?.focus();
        }
    }, [confirmationOpen]);

    const [shippingIdValidation, setShippingIdValidation] = useState<"idle" | "success" | "error">("error");

    const includedRecords =
        sourceRecords.filter(
            (record) => record.IsIncluded
        );

    const skippedRecords =
        sourceRecords.filter(
            (record) => !record.IsIncluded
        );

    const includedHolderCount =
        includedRecords.length;

    const verifiedHolderCount =
        includedRecords.filter(
            (record) =>
                record.Status.trim().toUpperCase() ===
                "VERIFIED"
        ).length;

    const allIncludedHoldersVerified =
        includedHolderCount > 0 &&
        verifiedHolderCount === includedHolderCount;

    const isDisassociateDisabled =
        shippingIdValidation !== "success" ||
        !allIncludedHoldersVerified ||
        isDisassociating;

    const holderProgressRadius = 7;

    const holderProgressCircumference =
        2 * Math.PI * holderProgressRadius;

    const holderVerificationRatio =
        includedHolderCount > 0
            ? Math.min(
                verifiedHolderCount /
                includedHolderCount,
                1
            )
            : 1;

    const holderProgressOffset =
        holderProgressCircumference *
        holderVerificationRatio;

    let totalTone = "";

    if (request.Total !== null) {
        if (
            preview.TotalQty >= request.Total &&
            preview.TotalQty <=
            preview.MaximumTotalQty
        ) {
            totalTone = "is-within-range";
        } else if (
            preview.TotalQty < request.Total
        ) {
            totalTone = "is-below-range";
        }
    }

    const handleShippingIdVerify = (
        event: FormEvent<HTMLFormElement>
    ) => {
        event.preventDefault();

        setShippingIdValidation(
            shippingId.trim().length > 0
                ? "success"
                : "error"
        );
    };

    const handleHolderVerify = (
        event: FormEvent<HTMLFormElement>
    ) => {
        event.preventDefault();

        const normalizedHolder =
            holder.trim().toUpperCase();

        if (!normalizedHolder) {
            return;
        }

        const isIncludedHolder =
            sourceRecords.some(
                (record) =>
                    record.IsIncluded &&
                    record.Holder.trim().toUpperCase() ===
                    normalizedHolder
            );

        if (!isIncludedHolder) {
            setHolderNotFound(true);
            return;
        }

        setHolderNotFound(false);

        /*
         * Select only one matching, unverified included row.
         * This guarantees that one successful submission
         * increases the verified count by exactly one.
         */
        const matchingRecordIndex =
            sourceRecords.findIndex(
                (record) =>
                    record.IsIncluded &&
                    record.Status.trim().toUpperCase() !==
                    "VERIFIED" &&
                    record.Holder.trim().toUpperCase() ===
                    normalizedHolder
            );

        if (matchingRecordIndex < 0) {
            setHolder("");
            return;
        }

        const matchingIncludedRecordIndex =
            sourceRecords
                .slice(
                    0,
                    matchingRecordIndex + 1
                )
                .filter(
                    (record) =>
                        record.IsIncluded
                ).length - 1;

        setSourceRecords((current) =>
            current.map((record, index) =>
                index === matchingRecordIndex
                    ? {
                        ...record,
                        Status: "VERIFIED",
                    }
                    : record
            )
        );

        setFocusedIncludedRecordIndex(
            matchingIncludedRecordIndex
        );

        setHolder("");
    };

    const handleHolderChange = (
        value: string
    ) => {
        setHolder(value);

        if (
            holderNotFound &&
            sourceRecords.some(
                (record) =>
                    record.IsIncluded &&
                    record.Holder.trim().toUpperCase() ===
                    value.trim().toUpperCase()
            )
        ) {
            setHolderNotFound(false);
        }
    };

    const openDisassociationConfirmation = () => {
        setDisassociateSubmitError("");
        setConfirmationOpen(true);
    };

    const closeDisassociationConfirmation = () => {
        if (!isDisassociating) {
            setConfirmationOpen(false);
            setDisassociateSubmitError("");
        }
    };

    const handleConfirmedDisassociation =
        async () => {
            if (isDisassociating) {
                return;
            }

            const holders = includedRecords
                .map((record) => record.Holder.trim())
                .filter(Boolean);

            setIsDisassociating(true);
            setDisassociateSubmitError("");

            try {
                await onDisassociate(shippingId.trim(), holders);
                onClose();
            } catch (error: unknown) {
                setDisassociateSubmitError(
                    error instanceof Error
                        ? error.message
                        : "Unable to disassociate the included Holders."
                );

                setIsDisassociating(false);
            }
        };

    const handleBackdropMouseDown = (
        event: MouseEvent<HTMLDivElement>
    ) => {
        if (
            event.target === event.currentTarget &&
            !isDisassociating
        ) {
            onClose();
        }
    };

    return (
        <div
            className="modal d-block withdrawal-disassociation-backdrop"
            role="dialog"
            aria-modal="true"
            aria-labelledby="withdrawal-disassociation-title"
            onMouseDown={handleBackdropMouseDown}
        >
            <div className="modal-dialog modal-xl modal-dialog-centered modal-dialog-scrollable withdrawal-disassociation-dialog">
                <div className="modal-content">
                    <div className="modal-header">
                        <h2
                            id="withdrawal-disassociation-title"
                            className="modal-title"
                        >
                            WITHDRAWAL DISASSOCIATION DETAILS
                        </h2>

                        <button
                            type="button"
                            className="btn-close"
                            aria-label="Close"
                            onClick={onClose}
                            disabled={isDisassociating}
                        />
                    </div>

                    <div className="modal-body withdrawal-disassociation-body">
                        <section className="withdrawal-disassociation-section">
                            <h3>SELECTED REQUEST</h3>

                            <div className="withdrawal-disassociation-summary-primary">
                                <div className="withdrawal-disassociation-field">
                                    <span>DATE</span>
                                    <strong>
                                        {formatShortDate(
                                            request.Date
                                        )}
                                    </strong>
                                </div>
                                <div className="withdrawal-disassociation-field">
                                    <span>REQUESTOR</span>
                                    <strong>
                                        {displayValue(
                                            request.Requestor
                                        )}
                                    </strong>
                                </div>
                                <div className="withdrawal-disassociation-field">
                                    <span>SHIFT</span>
                                    <strong>
                                        {displayValue(
                                            request.Shift
                                        )}
                                    </strong>
                                </div>
                                <div className="withdrawal-disassociation-field">
                                    <span>MODEL</span>
                                    <strong>
                                        {displayValue(
                                            request.Model
                                        )}
                                    </strong>
                                </div>
                                <div className="withdrawal-disassociation-field">
                                    <span>CATEGORY</span>
                                    <strong>
                                        {displayValue(
                                            request.Category
                                        )}
                                    </strong>
                                </div>
                                <div className="withdrawal-disassociation-field">
                                    <span>GRADE</span>
                                    <strong>
                                        {displayValue(
                                            request.Grade
                                        )}
                                    </strong>
                                </div>
                                <div className="withdrawal-disassociation-field">
                                    <span>
                                        SLIDERPARTNUMBER
                                    </span>
                                    <strong>
                                        {displayValue(
                                            request.SliderPartNumber
                                        )}
                                    </strong>
                                </div>
                                <div className="withdrawal-disassociation-field">
                                    <span>HEADTYPE</span>
                                    <strong>
                                        {displayValue(
                                            request.HeadType
                                        )}
                                    </strong>
                                </div>
                            </div>

                            <div className="withdrawal-disassociation-summary-secondary">
                                <div
                                    className={`withdrawal-disassociation-totals ${totalTone}`}
                                >
                                    <div>
                                        <span>TOTAL</span>
                                        <strong>
                                            {displayValue(
                                                request.Total
                                            )}
                                        </strong>
                                    </div>

                                    <span
                                        className="withdrawal-total-separator"
                                        aria-hidden="true"
                                    >
                                        |
                                    </span>

                                    <div>
                                        <span>TOTAL QTY</span>
                                        <strong>
                                            {displayValue(
                                                preview.TotalQty
                                            )}
                                        </strong>
                                    </div>
                                </div>

                                <div className="withdrawal-disassociation-field">
                                    <span>REMARKS</span>
                                    <strong>
                                        {displayValue(
                                            request.Remarks
                                        )}
                                    </strong>
                                </div>

                                <div className="withdrawal-disassociation-field">
                                    <span>
                                        ACKNOWLEDGEBY
                                    </span>
                                    <strong>
                                        {displayValue(
                                            request.AcknowledgeBy
                                        )}
                                    </strong>
                                </div>

                                <div className="withdrawal-disassociation-field">
                                    <span>
                                        ACTUALOUTPUT
                                    </span>
                                    <strong>
                                        {displayValue(
                                            request.ActualOutput
                                        )}
                                    </strong>
                                </div>

                                <div className="withdrawal-disassociation-field">
                                    <span>STATUS</span>
                                    <strong className="withdrawal-selected-status">
                                        {displayValue(
                                            request.Status
                                        )}
                                    </strong>
                                </div>
                            </div>
                        </section>

                        <section className="withdrawal-disassociation-section">
                            <h3>VERIFY HOLDERS</h3>

                            <div className="withdrawal-verify-grid">
                                <div className="withdrawal-verify-item">
                                    <label htmlFor="withdrawal-shipping-id">
                                        SHIPPING ID
                                    </label>

                                    <form
                                        className="withdrawal-verify-control"
                                        onSubmit={handleShippingIdVerify}
                                        noValidate
                                    >
                                        <input
                                            id="withdrawal-shipping-id"
                                            type="text"
                                            className={`form-control ${shippingIdValidation === "error"
                                                    ? "is-invalid"
                                                    : ""
                                                }`}
                                            value={shippingId}
                                            onChange={(event) =>
                                                setShippingId(event.target.value)
                                            }
                                            autoComplete="off"
                                            aria-invalid={
                                                shippingIdValidation === "error"
                                            }
                                            aria-describedby={
                                                shippingIdValidation === "idle"
                                                    ? undefined
                                                    : "withdrawal-shipping-id-feedback"
                                            }
                                        />

                                        <button
                                            type="submit"
                                            className="btn btn-primary withdrawal-verify-button"
                                        >
                                            VERIFY
                                        </button>
                                    </form>

                                    {shippingIdValidation === "success" && (
                                        <div
                                            id="withdrawal-shipping-id-feedback"
                                            className="withdrawal-shipping-id-feedback is-success"
                                            role="status"
                                            aria-live="polite"
                                        >
                                            <i
                                                className="fa-regular fa-circle-check"
                                                aria-hidden="true"
                                            />

                                            <span>Shipping ID verified.</span>
                                        </div>
                                    )}

                                    {shippingIdValidation === "error" && (
                                        <div
                                            id="withdrawal-shipping-id-feedback"
                                            className="withdrawal-shipping-id-feedback is-error"
                                            role="alert"
                                        >
                                            Shipping ID is required.
                                        </div>
                                    )}
                                </div>

                                <div className="withdrawal-verify-item">
                                    <label htmlFor="withdrawal-holder">
                                        HOLDER
                                    </label>

                                    <form
                                        className="withdrawal-verify-control"
                                        onSubmit={handleHolderVerify}
                                        noValidate
                                    >
                                        <input
                                            id="withdrawal-holder"
                                            type="text"
                                            className="form-control"
                                            value={holder}
                                            onChange={(event) =>
                                                handleHolderChange(
                                                    event.target.value
                                                )
                                            }
                                            onKeyDown={(event) => {
                                                if (event.key === "Enter") {
                                                    event.preventDefault();
                                                    event.currentTarget.form?.requestSubmit();
                                                }
                                            }}
                                            autoComplete="off"
                                            aria-invalid={holderNotFound}
                                            aria-describedby="withdrawal-holder-feedback"
                                        />

                                        <button
                                            type="submit"
                                            className="btn btn-primary withdrawal-verify-button"
                                        >
                                            VERIFY
                                        </button>
                                    </form>

                                    {holderNotFound ? (
                                        <div
                                            id="withdrawal-holder-feedback"
                                            className="withdrawal-holder-progress is-not-found"
                                            role="alert"
                                        >
                                            <i
                                                className="fa-regular fa-circle-xmark"
                                                aria-hidden="true"
                                            />

                                            <span>
                                                Holder not found in Included.
                                            </span>
                                        </div>
                                    ) : (
                                        <div
                                            id="withdrawal-holder-feedback"
                                            className={`withdrawal-holder-progress ${allIncludedHoldersVerified
                                                    ? "is-complete"
                                                    : "is-pending"
                                                }`}
                                            role="status"
                                            aria-live="polite"
                                            aria-label={`${verifiedHolderCount} out of ${includedHolderCount} holders verified`}
                                        >
                                            {allIncludedHoldersVerified ? (
                                                <i
                                                    className="fa-regular fa-circle-check"
                                                    aria-hidden="true"
                                                />
                                            ) : (
                                                <svg
                                                    className="withdrawal-holder-progress-ring"
                                                    viewBox="0 0 18 18"
                                                    aria-hidden="true"
                                                >
                                                    <circle
                                                        className="withdrawal-holder-progress-ring-track"
                                                        cx="9"
                                                        cy="9"
                                                        r={holderProgressRadius}
                                                    />

                                                    <circle
                                                        className="withdrawal-holder-progress-ring-value"
                                                        cx="9"
                                                        cy="9"
                                                        r={holderProgressRadius}
                                                        strokeDasharray={
                                                            holderProgressCircumference
                                                        }
                                                        strokeDashoffset={
                                                            holderProgressOffset
                                                        }
                                                        transform="rotate(-90 9 9)"
                                                    />
                                                </svg>
                                            )}

                                            <span>
                                                {verifiedHolderCount} out of{" "}
                                                {includedHolderCount} included holders verified.
                                            </span>
                                        </div>
                                    )}
                                </div>
                            </div>
                        </section>

                        <section className="withdrawal-disassociation-section">
                            <h3>
                                FIFO SOURCE RECORDS REVIEW
                            </h3>

                            <RecordsTable
                                title="INCLUDED IN TOTAL QTY"
                                tone="included"
                                records={includedRecords}
                                maximumTotalQty={preview.MaximumTotalQty}
                                targetTotal={preview.Total}
                                emptyMessage="No records were included."
                                focusedRecordIndex={focusedIncludedRecordIndex} 
                            />

                            <RecordsTable
                                title="SKIPPED BY LIMIT"
                                tone="skipped"
                                records={skippedRecords}
                                maximumTotalQty={ preview.MaximumTotalQty }
                                targetTotal={preview.Total}
                                emptyMessage="No records were skipped."
                            />

                            <div className="withdrawal-fifo-cap">
                                FIFO CAP = TOTAL{" "}
                                {preview.Total} +{" "}
                                {preview.Tolerance} ={" "}
                                {preview.MaximumTotalQty}
                            </div>
                        </section>
                    </div>

                    <div className="modal-footer withdrawal-disassociation-footer">
                        <button
                            type="button"
                            className="btn withdrawal-disassociate-button withdrawal-disassociation-submit"
                            disabled={isDisassociateDisabled}
                            onClick={
                                openDisassociationConfirmation
                            }
                        >
                            DISASSOCIATE
                        </button>
                    </div>
                </div>
            </div>

            {confirmationOpen && (
                <div
                    className="withdrawal-disassociation-confirmation-backdrop"
                    onMouseDown={(event) => {
                        if (
                            event.target ===
                            event.currentTarget
                        ) {
                            closeDisassociationConfirmation();
                        }
                    }}
                >
                    <section
                        className="withdrawal-disassociation-confirmation"
                        role="alertdialog"
                        aria-modal="true"
                        aria-labelledby="withdrawal-confirmation-title"
                        aria-describedby="withdrawal-confirmation-message"
                    >
                        <div className="withdrawal-disassociation-confirmation-header">
                            <div className="withdrawal-disassociation-confirmation-icon">
                                <i
                                    className="fa-solid fa-triangle-exclamation"
                                    aria-hidden="true"
                                />
                            </div>

                            <h3 id="withdrawal-confirmation-title">
                                CONFIRM DISASSOCIATION
                            </h3>
                        </div>

                        <p
                            id="withdrawal-confirmation-message"
                            className="withdrawal-disassociation-confirmation-message"
                        >
                            This will permanently remove the Holders
                            from STACKER data.
                        </p>

                        <p className="withdrawal-disassociation-confirmation-warning">
                            This action cannot be undone.
                        </p>

                        <div className="withdrawal-disassociation-confirmation-summary">
                            <div>
                                <span>HOLDERS</span>
                                <strong>
                                    {includedRecords.length}
                                </strong>
                            </div>

                            <div>
                                <span>TOTAL QTY</span>
                                <strong>
                                    {preview.TotalQty}
                                </strong>
                            </div>
                        </div>

                        {disassociateSubmitError && (
                            <div
                                className="alert alert-danger mt-3 mb-0"
                                role="alert"
                            >
                                {disassociateSubmitError}
                            </div>
                        )}

                        <div className="withdrawal-disassociation-confirmation-actions">
                            <button
                                ref={confirmationCancelRef}
                                type="button"
                                className="btn btn-outline-secondary"
                                disabled={isDisassociating}
                                onClick={
                                    closeDisassociationConfirmation
                                }
                            >
                                CANCEL
                            </button>

                            <button
                                type="button"
                                className="btn withdrawal-disassociate-button"
                                disabled={isDisassociating}
                                onClick={() =>
                                    void handleConfirmedDisassociation()
                                }
                            >
                                {isDisassociating && (
                                    <span
                                        className="spinner-border spinner-border-sm me-2"
                                        aria-hidden="true"
                                    />
                                )}

                                {isDisassociating
                                    ? "DISASSOCIATING..."
                                    : "YES, DISASSOCIATE"}
                            </button>
                        </div>
                    </section>
                </div>
            )}

        </div>
    );
}
