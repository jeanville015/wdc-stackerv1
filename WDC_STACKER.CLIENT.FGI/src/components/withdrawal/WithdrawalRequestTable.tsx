import { useState, type KeyboardEvent } from "react";
import type {
    FgiWithdrawalRequest,
} from "../../types/withdrawal";
import { getWithdrawalStatusInfo } from "../../utils/withdrawalStatus";

interface Props {
    rows: FgiWithdrawalRequest[];
    selectedRequest: FgiWithdrawalRequest | null;
    loading: boolean;
    error: string;
    onRequestSelected: (
        request: FgiWithdrawalRequest
    ) => void;
}

function formatShortDate(value: string | null): string {
    if (!value) {
        return "";
    }

    /*
     * The API returns a value such as:
     * 2026-07-22T00:00:00
     *
     * Splitting the date avoids timezone conversion changing the day.
     */
    const dateOnly = value.split("T")[0];
    const [year, month, day] = dateOnly.split("-");

    if (!year || !month || !day) {
        return value;
    }

    return `${month}/${day}/${year}`;
}

function hasValue(value: string): boolean {
    return value.trim().length > 0;
}

export default function WithdrawalRequestTable({
    rows,
    selectedRequest,
    loading,
    error,
    onRequestSelected,
}: Props) {
    const [expandedRequestIds, setExpandedRequestIds] =
        useState<Set<number>>(new Set());
    const [statusFilter, setStatusFilter] = useState<string>("ALL");
    const [hidePastRequests, setHidePastRequests] = useState<boolean>(false);

    const filteredRows = rows.filter((row) => {
        if (statusFilter === "ALL") return true;
        const statusInfo = getWithdrawalStatusInfo(row.Status, row.ActualOutput, row.Total);
        return statusInfo.label === statusFilter;
    }).filter((row) => {
        if (!hidePastRequests) return true;
        if (!row.Date) return false;
        const requestDate = new Date(row.Date.split("T")[0]);
        const today = new Date();
        today.setHours(0, 0, 0, 0);
        return requestDate >= today;
    });

    const toggleExpanded = (requestId: number) => {
        setExpandedRequestIds((previous) => {
            const next = new Set(previous);

            if (next.has(requestId)) {
                next.delete(requestId);
            } else {
                next.add(requestId);
            }

            return next;
        });
    };

    const handleCardKeyDown = (
        event: KeyboardEvent<HTMLElement>,
        row: FgiWithdrawalRequest
    ) => {
        if (event.key !== "Enter" && event.key !== " ") {
            return;
        }

        event.preventDefault();
        onRequestSelected(row);
    };

    if (loading) {
        return (
            <section className="withdrawal-request-panel">
                <div className="withdrawal-placeholder">
                    <span
                        className="spinner-border spinner-border-sm"
                        role="status"
                        aria-hidden="true"
                    />

                    <span>Loading withdrawal requests...</span>
                </div>
            </section>
        );
    }

    if (error) {
        return (
            <section className="withdrawal-request-panel">
                <div
                    className="withdrawal-placeholder"
                    role="alert"
                    style={{ color: "#d23232" }}
                >
                    {error}
                </div>
            </section>
        );
    }

    return (
        <section className="withdrawal-request-panel">
            <div className="withdrawal-request-header">
                <h3>KITTING REQUESTS</h3>
                <div className="withdrawal-request-filter">
                    <select
                        value={statusFilter}
                        onChange={(e) => setStatusFilter(e.target.value)}
                        className="form-select form-select-sm"
                        style={{ width: "auto", display: "inline-block" }}
                    >
                        <option value="ALL">All Status</option>
                        <option value="OPEN">Open</option>
                        <option value="PARTIAL">Partial</option>
                        <option value="COMPLETED">Completed</option>
                        <option value="CLOSED">Closed</option>
                    </select>
                    <label className="form-check-label ms-3" style={{ fontSize: "0.85rem", cursor: "pointer" }}>
                        <input
                            type="checkbox"
                            className="form-check-input me-1"
                            checked={hidePastRequests}
                            onChange={(e) => setHidePastRequests(e.target.checked)}
                        />
                        Hide past requests
                    </label>
                </div>
            </div>
            <div className="withdrawal-request-scroll">
                <div className="withdrawal-request-cards">
                    {filteredRows.map((row) => {
                        const isSelected =
                            selectedRequest?.RequestId === row.RequestId;

                        const isExpanded =
                            expandedRequestIds.has(row.RequestId);

                        return (
                            <article
                                key={row.RequestId}
                                className={
                                    isSelected
                                        ? "withdrawal-request-card is-selected"
                                        : "withdrawal-request-card"
                                }
                                tabIndex={0}
                                aria-selected={isSelected}
                                onClick={() =>
                                    onRequestSelected(row)
                                }
                                onKeyDown={(event) =>
                                    handleCardKeyDown(
                                        event,
                                        row
                                    )
                                }
                            >
                                <header className="withdrawal-request-card-header">
                                    <div className="withdrawal-request-card-main">
                                        <strong className="withdrawal-request-card-grade">
                                            {row.Grade}
                                        </strong>

                                        <span className="withdrawal-request-card-partnum">
                                            {row.SliderPartNumber}
                                        </span>

                                        <span className="withdrawal-request-card-qty">
                                            QTY: {row.Total ?? "—"}
                                        </span>

                                        {hasValue(row.Lec) && (
                                            <span className="withdrawal-request-card-tag">
                                                LEC: {row.Lec}
                                            </span>
                                        )}

                                        {hasValue(row.PenNum) && (
                                            <span className="withdrawal-request-card-tag">
                                                PENNUM: {row.PenNum}
                                            </span>
                                        )}

                                        {(() => {
                                            const statusInfo = getWithdrawalStatusInfo(
                                                row.Status,
                                                row.ActualOutput,
                                                row.Total
                                            );

                                            return (
                                                <span
                                                    className="withdrawal-status-pill"
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

                                    <div className="withdrawal-request-card-meta">
                                        <span>
                                            {formatShortDate(
                                                row.Date
                                            )}
                                        </span>

                                        <span>{row.Requestor}</span>
                                    </div>

                                    <button
                                        type="button"
                                        className="btn btn-sm withdrawal-request-card-toggle"
                                        aria-expanded={isExpanded}
                                        onClick={(event) => {
                                            event.stopPropagation();
                                            toggleExpanded(
                                                row.RequestId
                                            );
                                        }}
                                    >
                                        {isExpanded ? "▲" : "▼"}
                                    </button>
                                </header>

                                {isExpanded && (
                                    <div className="withdrawal-request-card-details">
                                        <div className="withdrawal-request-card-field">
                                            <span>SHIFT</span>
                                            <strong>
                                                {row.Shift || "—"}
                                            </strong>
                                        </div>

                                        <div className="withdrawal-request-card-field">
                                            <span>MODEL</span>
                                            <strong>
                                                {row.Model || "—"}
                                            </strong>
                                        </div>

                                        <div className="withdrawal-request-card-field">
                                            <span>CATEGORY</span>
                                            <strong>
                                                {row.Category || "—"}
                                            </strong>
                                        </div>

                                        <div className="withdrawal-request-card-field">
                                            <span>HEADTYPE</span>
                                            <strong>
                                                {row.HeadType || "—"}
                                            </strong>
                                        </div>

                                        <div className="withdrawal-request-card-field">
                                            <span>REMARKS</span>
                                            <strong>
                                                {row.Remarks || "—"}
                                            </strong>
                                        </div>

                                        <div className="withdrawal-request-card-field">
                                            <span>ACKNOWLEDGEBY</span>
                                            <strong>
                                                {row.AcknowledgeBy ||
                                                    "—"}
                                            </strong>
                                        </div>

                                        <div className="withdrawal-request-card-field">
                                            <span>ACTUALOUTPUT</span>
                                            <strong>
                                                {row.ActualOutput ??
                                                    "—"}
                                            </strong>
                                        </div>

                                        <div className="withdrawal-request-card-field">
                                            <span>STATUS</span>
                                            <strong>
                                                {row.Status || "—"}
                                            </strong>
                                        </div>
                                    </div>
                                )}
                            </article>
                        );
                    })}

                    {rows.length === 0 && (
                        <div className="text-center text-muted p-4">
                            No withdrawal requests were found.
                        </div>
                    )}
                </div>
            </div>
        </section>
    );
}