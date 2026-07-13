import { useEffect, useState } from "react";
import {
    disassociateHolderApi,
    getBoxAssignmentsApi,
} from "../../api/stackerApi";
import { useAuth } from "../../context/AuthContext";
import type { BoxAssignment, BoxView } from "../../types/stacker";

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

    useEffect(() => {
        if (!user?.token) {
            setError("Login token is missing.");
            setLoading(false);
            return;
        }

        getBoxAssignmentsApi(boxName, user.token)
            .then(setRows)
            .catch((err: unknown) =>
                setError(err instanceof Error ? err.message : "Load failed.")
            )
            .finally(() => setLoading(false));
    }, [boxName, user?.token]);

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
                    <div className="modal-header">
                        <h5 className="modal-title">
                            Box Assignments: {boxName}
                        </h5>
                        <button
                            type="button"
                            className="btn-close"
                            aria-label="Close"
                            onClick={onClose}
                        />
                    </div>

                    <div className="modal-body">
                        {error && (
                            <div className="alert alert-danger">{error}</div>
                        )}

                        {loading ? (
                            <div className="text-center p-4">
                                <span className="spinner-border" />
                            </div>
                        ) : (
                            <div className="table-responsive">
                                <table className="table table-bordered table-hover align-middle">
                                    <thead className="table-light">
                                        <tr>
                                            <th>Holder</th>
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
                                                    <td>{row.ProductName}</td>
                                                    <td>{row.Factory}</td>
                                                    <td>{row.Lec}</td>
                                                    <td>{row.Status}</td>
                                                    <td>
                                                        <button
                                                            className="btn btn-sm btn-danger"
                                                            disabled={!canDisassociate}
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
                                                    colSpan={6}
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