import { useEffect, useState } from "react";
import {
    disassociateHolderApi,
    getBoxAssignmentsApi,
} from "../../api/stackerApi";
import { useAuth } from "../../context/useAuth";
import type { BoxAssignment, BoxView } from "../../types/stacker";
import {
    formatBoxName,
    formatRackName,
} from "../../utils/nameTransformers";

interface Props {
    boxName: string;
    rackNumber: number;
    onClose: () => void;
    onBoxesChanged: (boxes: BoxView[]) => void;
}

export default function BoxAssignmentsModal({
    boxName,
    rackNumber,
    onClose,
    onBoxesChanged,
}: Props) {
    const { user } = useAuth();
    const [rows, setRows] = useState<BoxAssignment[]>([]);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState("");
    const [confirmRow, setConfirmRow] = useState<BoxAssignment | null>(null);
    const [deleting, setDeleting] = useState(false);
    const token = user?.token;
    const authError = token ? "" : "Login token is missing.";
    const displayError = authError || error;
    const displayLoading = Boolean(token) && loading;

    useEffect(() => {
        if (!token) {
            return;
        }

        let isCancelled = false;

        Promise.resolve()
            .then(() => {
                if (isCancelled) return undefined;

                setLoading(true);
                setError("");
                return getBoxAssignmentsApi(boxName, token);
            })
            .then((assignments) => {
                if (!isCancelled && assignments) {
                    setRows(assignments);
                }
            })
            .catch((err: unknown) =>
                setError(err instanceof Error ? err.message : "Load failed.")
            )
            .finally(() => {
                if (!isCancelled) {
                    setLoading(false);
                }
            });

        return () => {
            isCancelled = true;
        };
    }, [boxName, token]);

    const disassociate = async () => {
        if (!confirmRow || !user?.token) return;

        setDeleting(true);
        setError("");

        try {
            const result = await disassociateHolderApi(
                confirmRow.Holder,
                user.token
            );

            setRows((current) =>
                current.filter((row) => row.Holder !== confirmRow.Holder)
            );
            onBoxesChanged(result.GridViewBoxes);
            setConfirmRow(null);
        } catch (err) {
            setError(
                err instanceof Error ? err.message : "Disassociate failed."
            );
        } finally {
            setDeleting(false);
        }
    };

    return (
    <>
        <div
            className="modal d-block"
            role="dialog"
            aria-modal="true"
            aria-labelledby="pwd-box-assignments-modal-title"
            style={{ background: "rgba(9, 30, 66, 0.55)" }}
            onMouseDown={onClose}
        >
            <div
                className="modal-dialog modal-xl modal-dialog-centered modal-dialog-scrollable pwd-box-assignments-dialog"
                onMouseDown={(event) => event.stopPropagation()}
            >
                <div className="modal-content pwd-box-assignments-modal">
                    <div className="modal-header pwd-box-assignments-modal-header">
                        <div className="pwd-box-assignments-heading">
                            <span className="pwd-box-assignments-eyebrow">
                                Black Box
                            </span>

                            <h5
                                id="pwd-box-assignments-modal-title"
                                className="modal-title"
                            >
                                {formatBoxName(boxName, rackNumber)}
                            </h5>

                            <div className="pwd-box-assignments-subtitle">
                                <i
                                    className="fa-solid fa-chevron-right"
                                    aria-hidden="true"
                                />

                                <span>{formatRackName(rackNumber)}</span>
                            </div>
                        </div>

                        <button
                            type="button"
                            className="btn-close"
                            aria-label="Close"
                            onClick={onClose}
                        />
                    </div>

                    <div className="modal-body">
                        {displayError && (
                            <div className="alert alert-danger">{displayError}</div>
                        )}

                        {displayLoading ? (
                            <div className="text-center p-4">
                                <span className="spinner-border" />
                            </div>
                        ) : (
                            <div className="table-responsive pwd-box-assignments-table-wrap">
                                <table className="table table-bordered align-middle text-center pwd-box-assignments-table">
                                    <thead>
                                        <tr>
                                            <th>Holder</th>
                                            <th>Job</th>
                                            <th>Qty</th>
                                            <th>Product Name</th>
                                            <th>Factory</th>
                                            <th>LEC</th>
                                            <th>Status</th>
                                            <th>Action</th>
                                        </tr>
                                    </thead>
                                    <tbody>
                                        {rows.map((row) => {
                                            const normalizedStatus = (
                                                row.Status ?? ""
                                            ).trim().toUpperCase();

                                            const isRelease = normalizedStatus === "RELEASE";
                                            const isHold = normalizedStatus === "HOLD";
                                            const canDisassociate = isRelease;

                                            const rowClassName = [
                                                isRelease
                                                    ? "pwd-box-assignment-row--release"
                                                    : "",
                                                isHold
                                                    ? "pwd-box-assignment-row--hold"
                                                    : "",
                                            ]
                                                .filter(Boolean)
                                                .join(" ");

                                            return (
                                                <tr
                                                    key={row.Holder}
                                                    className={rowClassName || undefined}
                                                >
                                                    <td className="pwd-box-assignment-holder">
                                                        {row.Holder}
                                                    </td>

                                                    <td>{row.Job?.trim() || "—"}</td>

                                                    <td>{row.Qty ?? "—"}</td>

                                                    <td>{row.ProductName || "—"}</td>

                                                    <td>{row.Factory || "—"}</td>

                                                    <td>{row.Lec || "—"}</td>

                                                    <td>
                                                        {isRelease ? (
                                                            <span className="pwd-box-assignment-status pwd-box-assignment-status--release">
                                                                RELEASE
                                                            </span>
                                                        ) : isHold ? (
                                                            <span className="pwd-box-assignment-status pwd-box-assignment-status--hold">
                                                                HOLD
                                                            </span>
                                                        ) : (
                                                            <span className="pwd-box-assignment-empty-value">
                                                                {row.Status?.trim() || "—"}
                                                            </span>
                                                        )}
                                                    </td>

                                                    <td>
                                                        <button
                                                            type="button"
                                                            className={[
                                                                "btn",
                                                                "btn-sm",
                                                                "pwd-box-assignment-disassociate",
                                                                canDisassociate
                                                                    ? "btn-outline-danger"
                                                                    : "pwd-box-assignment-disassociate--disabled",
                                                            ].join(" ")}
                                                            disabled={!canDisassociate}
                                                            aria-label={
                                                                canDisassociate
                                                                    ? `Disassociate holder ${row.Holder}`
                                                                    : `Holder ${row.Holder} cannot be disassociated`
                                                            }
                                                            onClick={() =>
                                                                setConfirmRow(row)
                                                            }
                                                        >
                                                            <i
                                                                className="fa-solid fa-link-slash"
                                                                aria-hidden="true"
                                                            />

                                                            <span>Disassociate</span>
                                                        </button>
                                                    </td>
                                                </tr>
                                            );
                                        })}

                                        {rows.length === 0 && (
                                            <tr>
                                                <td
                                                    colSpan={8}
                                                    className="text-center text-muted p-4"
                                                >
                                                    No assignments found.
                                                </td>
                                            </tr>
                                        )}
                                    </tbody>
                                </table>
                            </div>
                        )}

                            {confirmRow && (
                                <div
                                    className="modal d-block"
                                    role="dialog"
                                    aria-modal="true"
                                    style={{ background: "rgba(9, 30, 66, 0.55)", zIndex: 1060 }}
                                    onMouseDown={() => {
                                        if (!deleting) setConfirmRow(null);
                                    }}
                                >
                                    <div
                                        className="modal-dialog modal-dialog-centered"
                                        onMouseDown={(event) => event.stopPropagation()}
                                    >
                                        <div className="modal-content">
                                            <div className="modal-header">
                                                <h5 className="modal-title">Confirm Disassociate?</h5>
                                            </div>

                                            <div className="modal-body">
                                                <p className="mb-2">
                                                    Are you sure you want to disassociate the Holder{" "}
                                                    <strong>{confirmRow.Holder}</strong> from{" "}
                                                    <strong>{boxName}</strong>?
                                                </p>
                                                <p className="mb-0 text-danger">
                                                    This action cannot be undone.
                                                </p>
                                            </div>

                                            <div className="modal-footer">
                                                <button
                                                    className="btn btn-secondary"
                                                    disabled={deleting}
                                                    onClick={() => setConfirmRow(null)}
                                                >
                                                    Cancel
                                                </button>
                                                <button
                                                    className="btn btn-danger"
                                                    disabled={deleting}
                                                    onClick={disassociate}
                                                >
                                                    {deleting ? "Processing..." : "Disassociate"}
                                                </button>
                                            </div>
                                        </div>
                                    </div>
                                </div>
                            )}
                    </div>

                    <div className="modal-footer pwd-box-assignments-modal-footer">
                        <button className="btn btn-secondary" onClick={onClose}>
                            Close
                        </button>
                    </div>
                </div>
            </div>
        </div>
    </>
    );
}
