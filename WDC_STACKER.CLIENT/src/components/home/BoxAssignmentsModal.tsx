import { useEffect, useState } from "react";
import {
    disassociateHolderApi,
    getBoxAssignmentsApi,
} from "../../api/stackerApi";
import { useAuth } from "../../context/useAuth";
import type { BoxAssignment, BoxView } from "../../types/stacker";
import { formatBoxName } from "../../utils/nameTransformers";

interface Props {
    boxName: string;
    onClose: () => void;
    onBoxesChanged: (boxes: BoxView[]) => void;
}

export default function BoxAssignmentsModal({
    boxName,
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
            style={{ background: "rgba(9, 30, 66, 0.55)" }}
            onMouseDown={onClose}
        >
            <div
                className="modal-dialog modal-xl modal-dialog-centered modal-dialog-scrollable"
                onMouseDown={(event) => event.stopPropagation()}
            >
                <div className="modal-content">
                    <div className="modal-header align-items-center">
                        <h5 className="modal-title" style={{ flex: 1, textAlign: "center" }}>
                            Black Box: {formatBoxName(boxName, 0)}
                        </h5>
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
                            <div className="table-responsive">
                                <table className="table table-bordered table-hover align-middle">
                                    <thead className="table-light">
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
                                            const canDisassociate =
                                                row.Status.trim().toUpperCase() ===
                                                "RELEASE";

                                            return (
                                                <tr key={row.Holder}>
                                                    <td>{row.Holder}</td>
                                                    <td>{row.Job ?? ""}</td>
                                                    <td>{row.Qty ?? ""}</td>
                                                    <td>{row.ProductName}</td>
                                                    <td>{row.Factory}</td>
                                                    <td>{row.Lec}</td>
                                                    <td>{row.Status}</td>
                                                    <td>
                                                        <button
                                                            className={`btn btn-sm ${
                                                                canDisassociate
                                                                    ? "btn-danger"
                                                                    : "btn-secondary"
                                                            }`}
                                                            disabled={!canDisassociate}
                                                            style={
                                                                !canDisassociate
                                                                    ? {
                                                                          backgroundColor: "#adb5bd",
                                                                          borderColor: "#adb5bd",
                                                                          color: "#f1f1f1",
                                                                          opacity: 1,
                                                                          cursor: "not-allowed",
                                                                      }
                                                                    : undefined
                                                            }
                                                            onClick={() =>
                                                                setConfirmRow(row)
                                                            }
                                                        >
                                                            Disassociate
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

                    <div className="modal-footer">
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
