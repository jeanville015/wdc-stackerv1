import type { FgiWithdrawalRequest } from "../../types/withdrawal";
import { getWithdrawalStatusInfo } from "../../utils/withdrawalStatus";

interface Props {
    request: FgiWithdrawalRequest;
    actionError: string;
    acknowledgingRequestId: number | null;
    disassociationLoadingRequestId: number | null;
    onAcknowledge: (
        request: FgiWithdrawalRequest
    ) => Promise<void>;
    onWithdraw: (
        request: FgiWithdrawalRequest
    ) => Promise<void>;
    actionSuccess: string;
}

function formatShortDate(value: string | null): string {
    if (!value) {
        return "—";
    }

    const dateOnly = value.split("T")[0];
    const [year, month, day] = dateOnly.split("-");

    if (!year || !month || !day) {
        return value;
    }

    return `${month}/${day}/${year}`;
}

function displayValue(
    value: string | number | null
): string | number {
    if (value === null || value === "") {
        return "—";
    }

    return value;
}

export default function SelectedWithdrawalRequestPanel({
    request,
    actionError,
    acknowledgingRequestId,
    disassociationLoadingRequestId,
    onAcknowledge,
    onWithdraw,
    actionSuccess,
}: Props) {
    const isAcknowledging =
        acknowledgingRequestId === request.RequestId;

    const isPreparingWithdrawal =
        disassociationLoadingRequestId ===
        request.RequestId;

    const isAcknowledged =
        Boolean(request.AcknowledgeBy.trim());

    const isClosed =
        request.Status.trim().toUpperCase() === "CLOSED" ||
        request.Status.trim().toUpperCase() === "COMPLETED";

    return (
        <article className="withdrawal-selected-request">
            <header className="withdrawal-selected-header">
                <h3>SELECTED WITHDRAWAL REQUEST</h3>
            </header>

            {actionError && (
                <div
                    className="alert alert-danger mb-3"
                    role="alert"
                >
                    {actionError}
                </div>
            )}

            {actionSuccess && (
                <div
                    className="alert alert-success mb-3"
                    role="status"
                >
                    {actionSuccess}
                </div>
            )}

            <div className="withdrawal-selected-body">
                <div className="withdrawal-selected-info">
                <div className="withdrawal-selected-primary">
                    <div className="withdrawal-selected-primary-main">
                        <strong className="withdrawal-selected-grade">
                            {displayValue(request.Grade)}
                        </strong>

                        <strong className="withdrawal-selected-partnum">
                            {displayValue(
                                request.SliderPartNumber
                            )}
                        </strong>

                        <span className="withdrawal-selected-qty">
                            QTY: {displayValue(request.Total)}
                        </span>

                        {(() => {
                            const statusInfo = getWithdrawalStatusInfo(
                                request.Status,
                                request.ActualOutput,
                                request.Total
                            );

                            return (
                                <span
                                    className="withdrawal-status-pill withdrawal-status-pill-large"
                                    style={{
                                        color: statusInfo.color,
                                        backgroundColor: statusInfo.backgroundColor,
                                    }}
                                >
                                    {statusInfo.label}
                                </span>
                            );
                        })()}
                    </div>

                    <div className="withdrawal-selected-primary-meta">
                        <span>
                            {displayValue(request.Requestor)}
                        </span>

                        <span>
                            {formatShortDate(request.Date)}
                        </span>
                    </div>
                </div>

                <div className="withdrawal-selected-fields">
                    {request.Lec.trim() && (
                        <div className="withdrawal-selected-field">
                            <span>LEC</span>
                            <strong>
                                {displayValue(request.Lec)}
                            </strong>
                        </div>
                    )}

                    {request.PenNum.trim() && (
                        <div className="withdrawal-selected-field">
                            <span>PENNUM</span>
                            <strong>
                                {displayValue(request.PenNum)}
                            </strong>
                        </div>
                    )}

                    <div className="withdrawal-selected-field">
                        <span>SHIFT</span>
                        <strong>
                            {displayValue(request.Shift)}
                        </strong>
                    </div>

                    <div className="withdrawal-selected-field">
                        <span>MODEL</span>
                        <strong>
                            {displayValue(request.Model)}
                        </strong>
                    </div>

                    <div className="withdrawal-selected-field">
                        <span>CATEGORY</span>
                        <strong>
                            {displayValue(request.Category)}
                        </strong>
                    </div>

                    <div className="withdrawal-selected-field">
                        <span>HEADTYPE</span>
                        <strong>
                            {displayValue(request.HeadType)}
                        </strong>
                    </div>

                    <div className="withdrawal-selected-field">
                        <span>REMARKS</span>
                        <strong>
                            {displayValue(request.Remarks)}
                        </strong>
                    </div>

                    <div className="withdrawal-selected-field">
                        <span>ACKNOWLEDGEBY</span>
                        <strong>
                            {displayValue(
                                request.AcknowledgeBy
                            )}
                        </strong>
                    </div>

                    <div className="withdrawal-selected-field">
                        <span>ACTUALOUTPUT</span>
                        <strong>
                            {displayValue(
                                request.ActualOutput
                            )}
                        </strong>
                    </div>

                    <div className="withdrawal-selected-field">
                        <span>STATUS</span>
                        <strong className="withdrawal-selected-status">
                            {displayValue(request.Status)}
                        </strong>
                    </div>
                </div>
                </div>

                <div className="withdrawal-selected-actions">
                    <span>ACTIONS</span>

                    <div className="withdrawal-selected-action-buttons">
                        {!isAcknowledged ? (
                            <button
                                type="button"
                                className="btn withdrawal-acknowledge-button"
                                disabled={isAcknowledging || isClosed}
                                onClick={() =>
                                    void onAcknowledge(request)
                                }
                            >
                                {isAcknowledging
                                    ? "ACKNOWLEDGING..."
                                    : "ACKNOWLEDGE"}
                            </button>
                        ) : (
                            <button
                                type="button"
                                className="btn withdrawal-disassociate-button"
                                disabled={
                                    isAcknowledging ||
                                    isPreparingWithdrawal ||
                                    isClosed
                                }
                                onClick={() =>
                                    void onWithdraw(request)
                                }
                            >
                                {isPreparingWithdrawal
                                    ? "CHECKING..."
                                    : "WITHDRAW"}
                            </button>
                        )}
                    </div>
                </div>
            </div>
        </article>
    );
}
