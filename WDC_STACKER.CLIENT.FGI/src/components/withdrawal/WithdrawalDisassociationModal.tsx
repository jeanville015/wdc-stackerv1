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

    onWithdraw: (

        includedHolders: string[],

        shippingId: string

    ) => Promise<void>;

    onVerifyShippingId: (

        shippingId: string

    ) => Promise<{ success: boolean; message: string }>;

}



interface RecordsTableProps {

    title: string;

    tone: "included" | "skipped";

    records: FgiWithdrawalSourceRecord[];

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

    emptyMessage,

    focusedRecordIndex = null,

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
                                                : "—"}

                                </td>



                                <td aria-label="Status">

                                    {record.IsIncluded &&

                                        record.Status && (

                                            <span

                                                className={`withdrawal-fifo-status ${record.Status === "VERIFIED"

                                                    ? "is-verified"

                                                    : record.Status === "IN-SITE HOLD"

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

    onWithdraw,

    onVerifyShippingId,

}: Props) {

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

    // Check if request is already fulfilled
    const isRequestFulfilled =
        request.ActualOutput !== null &&
        request.Total !== null &&
        request.ActualOutput >= request.Total;



    const isRequestClosed =

        request.Status.trim().toUpperCase() === "CLOSED";



    const [

        confirmationOpen,

        setConfirmationOpen,

    ] = useState(false);



    const [

        isWithdrawing,

        setIsWithdrawing,

    ] = useState(false);



    const [

        withdrawSubmitError,

        setWithdrawSubmitError,

    ] = useState("");



    const [

        shippingId,

        setShippingId,

    ] = useState("");



    const [

        shippingIdVerified,

        setShippingIdVerified,

    ] = useState(false);



    const [

        shippingIdVerifying,

        setShippingIdVerifying,

    ] = useState(false);



    const [

        shippingIdError,

        setShippingIdError,

    ] = useState("");



    const confirmationCancelRef =

        useRef<HTMLButtonElement | null>(null);



    useEffect(() => {

        if (confirmationOpen) {

            confirmationCancelRef.current?.focus();

        }

    }, [confirmationOpen]);






    const includedRecords =

        sourceRecords.filter(

            (record) => record.IsIncluded

        );



    const skippedRecords =

        sourceRecords.filter(

            (record) => !record.IsIncluded && record.WasReviewedForHold

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



    const isWithdrawDisabled =

        !allIncludedHoldersVerified ||

        !shippingIdVerified ||

        isWithdrawing ||

        isRequestFulfilled;



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



    const handleShippingIdChange = (

        value: string

    ) => {

        setShippingId(value);

        setShippingIdVerified(false);

        setShippingIdError("");

    };



    const handleShippingIdVerify = async (

        event: FormEvent<HTMLFormElement>

    ): Promise<void> => {

        event.preventDefault();



        const normalizedShippingId = shippingId.trim();



        if (!normalizedShippingId || shippingIdVerifying) {

            return;

        }



        setShippingIdVerifying(true);

        setShippingIdError("");



        const result = await onVerifyShippingId(

            normalizedShippingId

        );



        setShippingIdVerifying(false);



        if (!result.success) {

            setShippingIdVerified(false);

            setShippingIdError(result.message);

            return;

        }



        setShippingIdVerified(true);

        setShippingIdError("");

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



    const openWithdrawalConfirmation = () => {

        setWithdrawSubmitError("");

        setConfirmationOpen(true);

    };



    const closeWithdrawalConfirmation = () => {

        if (!isWithdrawing) {

            setConfirmationOpen(false);

            setWithdrawSubmitError("");

        }

    };



    const handleConfirmedWithdrawal =

        async () => {

            if (isWithdrawing) {

                return;

            }



            const holders = includedRecords

                .map((record) => record.Holder.trim())

                .filter(Boolean);



            setIsWithdrawing(true);

            setWithdrawSubmitError("");



            try {

                await onWithdraw(holders, shippingId.trim());

                onClose();

            } catch (error: unknown) {

                setWithdrawSubmitError(

                    error instanceof Error

                        ? error.message

                        : "Unable to withdraw the included Holders."

                );



                setIsWithdrawing(false);

            }

        };



    const handleBackdropMouseDown = (

        event: MouseEvent<HTMLDivElement>

    ) => {

        if (

            event.target === event.currentTarget &&

            !isWithdrawing

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

                    {isWithdrawing && (

                        <div

                            className="withdrawal-disassociation-loading-overlay"

                            role="status"

                            aria-live="polite"

                        >

                            <span className="spinner-border" aria-hidden="true" />

                            <span className="withdrawal-disassociation-loading-text">

                                DISASSOCIATING...

                            </span>

                        </div>

                    )}

                    <div className="modal-header">

                        <h2

                            id="withdrawal-disassociation-title"

                            className="modal-title"

                        >

                            WITHDRAWAL DETAILS

                        </h2>



                        <button

                            type="button"

                            className="btn-close"

                            aria-label="Close"

                            onClick={onClose}

                            disabled={isWithdrawing}

                        />

                    </div>



                    <div className="modal-body withdrawal-disassociation-body">

                        <section className="withdrawal-disassociation-section">

                            <h3>SELECTED REQUEST</h3>



                            <div className="withdrawal-disassociation-summary-primary">

                                <div className="withdrawal-disassociation-primary-main">

                                    <div className="withdrawal-disassociation-primary-row">

                                        <strong className="withdrawal-selected-grade">

                                            {displayValue(

                                                request.Grade

                                            )}

                                        </strong>

                                        <strong className="withdrawal-selected-partnum">

                                            {displayValue(

                                                request.SliderPartNumber

                                            )}

                                        </strong>

                                    </div>

                                    {(request.Lec.trim() || request.PenNum.trim()) && (

                                    <div className="withdrawal-disassociation-primary-tags">

                                        {request.Lec.trim() && (

                                        <span className="withdrawal-request-card-tag">

                                            LEC: {request.Lec}

                                        </span>

                                        )}

                                        {request.PenNum.trim() && (

                                        <span className="withdrawal-request-card-tag">

                                            PENNUM: {request.PenNum}

                                        </span>

                                        )}

                                    </div>

                                    )}

                                    <div className="withdrawal-disassociation-primary-meta">

                                        <span>

                                            {displayValue(

                                                request.Requestor

                                            )}

                                        </span>

                                        <span>

                                            {formatShortDate(

                                                request.Date

                                            )}

                                        </span>

                                    </div>

                                </div>

                                <div

                                    className={`withdrawal-disassociation-totals withdrawal-disassociation-totals-focal ${totalTone}`}

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

                            </div>



                            <div className="withdrawal-disassociation-summary-secondary">

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

                                    <span>HEADTYPE</span>

                                    <strong>

                                        {displayValue(

                                            request.HeadType

                                        )}

                                    </strong>

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

                            {isRequestFulfilled && (
                                <div
                                    className="alert alert-info mb-3"
                                    role="status"
                                >
                                    This request is already fulfilled. Actual output ({request.ActualOutput}) meets or exceeds the requested total ({request.Total}). No additional holders can be withdrawn.
                                </div>
                            )}

                            <div className="withdrawal-verify-grid">


                                <div className="withdrawal-verify-item">

                                    <label htmlFor="withdrawal-shipping-id">

                                        SHIPPING BOX ID

                                    </label>



                                    <form

                                        className="withdrawal-verify-control"

                                        onSubmit={handleShippingIdVerify}

                                        noValidate

                                    >

                                        <input

                                            id="withdrawal-shipping-id"

                                            type="text"

                                            className="form-control"

                                            value={shippingId}

                                            disabled={isWithdrawing || shippingIdVerifying}

                                            onChange={(event) =>

                                                handleShippingIdChange(

                                                    event.target.value

                                                )

                                            }

                                            autoComplete="off"

                                            aria-invalid={Boolean(shippingIdError)}

                                            aria-describedby="withdrawal-shipping-id-feedback"

                                        />



                                        <button

                                            type="submit"

                                            className="btn btn-primary withdrawal-verify-button"

                                            disabled={

                                                isWithdrawing ||

                                                shippingIdVerifying ||

                                                !shippingId.trim()

                                            }

                                        >

                                            {shippingIdVerifying ? "VERIFYING..." : "VERIFY"}

                                        </button>

                                    </form>



                                    <div

                                        id="withdrawal-shipping-id-feedback"

                                        className={`withdrawal-holder-progress ${shippingIdError
                                                ? "is-not-found"
                                                : shippingIdVerified
                                                    ? "is-complete"
                                                    : "is-pending"
                                            }`}

                                        role={shippingIdError ? "alert" : "status"}

                                        aria-live="polite"

                                    >

                                        <i

                                            className={
                                                shippingIdError
                                                    ? "fa-regular fa-circle-xmark"
                                                    : shippingIdVerified
                                                        ? "fa-regular fa-circle-check"
                                                        : "fa-solid fa-triangle-exclamation"
                                            }

                                            aria-hidden="true"

                                        />



                                        <span>

                                            {shippingIdError ||

                                                (shippingIdVerified

                                                    ? "Shipping Box Id verified."

                                                    : "Shipping Box Id not verified.")}

                                        </span>

                                    </div>

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

                                emptyMessage="No records were included."

                                focusedRecordIndex={focusedIncludedRecordIndex}

                            />



                            <RecordsTable

                                title="SKIPPED BY HOLD"

                                tone="skipped"

                                records={skippedRecords}

                                emptyMessage="No records were skipped due to holds."

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

                            disabled={isWithdrawDisabled}

                            onClick={

                                isRequestClosed ? onClose : openWithdrawalConfirmation

                            }

                        >

                            {isRequestClosed ? "CLOSE" : "WITHDRAW"}

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

                            closeWithdrawalConfirmation();

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

                                CONFIRM WITHDRAWAL

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



                        {withdrawSubmitError && (

                            <div

                                className="alert alert-danger mt-3 mb-0"

                                role="alert"

                            >

                                {withdrawSubmitError}

                            </div>

                        )}



                        <div className="withdrawal-disassociation-confirmation-actions">

                            <button

                                ref={confirmationCancelRef}

                                type="button"

                                className="btn btn-outline-secondary"

                                disabled={isWithdrawing}

                                onClick={

                                    closeWithdrawalConfirmation

                                }

                            >

                                CANCEL

                            </button>



                            <button

                                type="button"

                                className="btn withdrawal-disassociate-button"

                                disabled={isWithdrawing || !shippingId.trim()}

                                onClick={() =>

                                    void handleConfirmedWithdrawal()

                                }

                            >

                                {isWithdrawing && (

                                    <span

                                        className="spinner-border spinner-border-sm me-2"

                                        aria-hidden="true"

                                    />

                                )}



                                {isWithdrawing

                                    ? "WITHDRAWING..."

                                    : "YES, WITHDRAW"}

                            </button>

                        </div>

                    </section>

                </div>

            )}



        </div>

    );

}

