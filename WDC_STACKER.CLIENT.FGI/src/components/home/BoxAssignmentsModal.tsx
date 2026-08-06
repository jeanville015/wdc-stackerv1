import { useEffect, useState } from "react";
import { disassociateFgiHolder, getShipBoxAssignmentsApi } from "../../api/stackerApi";
import { useAuth } from "../../context/AuthContext";
import type { BoxAssignment, ShipBoxView } from "../../types/stacker";

interface Props {
    boxName: string;
    shipBox: ShipBoxView;
    onClose: () => void;
    onDisassociateSuccess?: () => void;
}

export default function BoxAssignmentsModal({
    boxName,
    shipBox,
    onClose,
    onDisassociateSuccess,
}: Props) {
    const { user } = useAuth();
    const hasToken = Boolean(user?.token);
    const [rows, setRows] = useState<BoxAssignment[]>([]);
    const [loading, setLoading] = useState(hasToken);
    const [error, setError] = useState("");
    const displayError = error || (!hasToken ? "Login token is missing." : "");
    const [disassociateHolder, setDisassociateHolder] = useState<BoxAssignment | null>(null);
    const [showSuccess, setShowSuccess] = useState(false);
    const [successMessage, setSuccessMessage] = useState("");
    const [disassociating, setDisassociating] = useState(false);
    const [disassociateError, setDisassociateError] = useState("");

    const shipBoxName = shipBox.ShipBoxName;

    useEffect(() => {
        if (!user?.token) {
            return;
        }

        getShipBoxAssignmentsApi(boxName, shipBoxName, user.token)
            .then((result) => {
                setError("");
                setRows(result);
            })
            .catch((err: unknown) =>
                setError(err instanceof Error ? err.message : "Load failed.")
            )
            .finally(() => setLoading(false));
    }, [boxName, shipBoxName, user?.token]);

    return (
        <>
            <div
                className="modal d-block"
                role="dialog"
                aria-modal="true"
                style={{ background: "rgba(9, 30, 66, 0.55)", zIndex: 1060 }}
                onMouseDown={onClose}
            >
                <div
                    className="modal-dialog modal-xl modal-dialog-centered modal-dialog-scrollable"
                    onMouseDown={(event) => event.stopPropagation()}
                >
                    <div className="modal-content">
                        <div className="modal-header">
                            <div className="stacker-modal-header-info">
                                <h5 className="modal-title">
                                    Ship Box: {shipBoxName}
                                </h5>

                                <div className="stacker-detail-pills">
                                    <span className="stacker-detail-pill">
                                        <strong>LEC:</strong> {shipBox.Lec}
                                    </span>
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
                            {displayError && <div className="alert alert-danger">{displayError}</div>}

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
                                                <th>Partnum</th>
                                                <th>Pennum</th>
                                                <th>Status</th>
                                                <th>Action</th>
                                            </tr>
                                        </thead>
                                        <tbody>
                                            {rows.map((row) => (
                                                <tr
                                                    key={row.Holder}
                                                    className={
                                                        row.Status === "HOLD"
                                                            ? "table-danger"
                                                            : ""
                                                    }
                                                >
                                                    <td>{row.Holder}</td>
                                                    <td>{row.ProductName}</td>
                                                    <td>{row.Factory}</td>
                                                    <td>{row.Lec}</td>
                                                    <td>{row.Partnum}</td>
                                                    <td>{row.Pennum}</td>
                                                    <td>{row.Status}</td>
                                                    <td>
                                                        {row.Status === "HOLD" && (
                                                            <button
                                                                type="button"
                                                                className="btn btn-danger btn-sm"
                                                                onClick={() => setDisassociateHolder(row)}
                                                            >
                                                                Disassociate
                                                            </button>
                                                        )}
                                                    </td>
                                                </tr>
                                            ))}

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
                        </div>

                        <div className="modal-footer">
                            <button className="btn btn-secondary" onClick={onClose}>
                                Close
                            </button>
                        </div>
                    </div>
                </div>
            </div>

            {disassociateHolder && (
                <div
                    className="modal d-block"
                    role="dialog"
                    aria-modal="true"
                    style={{ background: "rgba(9, 30, 66, 0.55)", zIndex: 1070 }}
                    onMouseDown={() => setDisassociateHolder(null)}
                >
                    <div
                        className="modal-dialog modal-dialog-centered"
                        onMouseDown={(event) => event.stopPropagation()}
                    >
                        <div className="modal-content">
                            <div className="modal-header">
                                <h5 className="modal-title">Confirm Disassociation</h5>
                                <button
                                    type="button"
                                    className="btn-close"
                                    aria-label="Close"
                                    onClick={() => setDisassociateHolder(null)}
                                />
                            </div>
                            <div className="modal-body">
                                <p>
                                    Are you sure you want to disassociate holder{" "}
                                    <strong>{disassociateHolder.Holder}</strong>?
                                </p>
                                {disassociateError && (
                                    <div className="alert alert-danger">{disassociateError}</div>
                                )}
                            </div>
                            <div className="modal-footer">
                                <button
                                    className="btn btn-secondary"
                                    disabled={disassociating}
                                    onClick={() => {
                                        setDisassociateHolder(null);
                                        setDisassociateError("");
                                    }}
                                >
                                    No
                                </button>
                                <button
                                    className="btn btn-danger"
                                    disabled={disassociating}
                                    onClick={async () => {
                                        if (!user?.token) {
                                            setDisassociateError("Login token is missing.");
                                            return;
                                        }

                                        const holderToDisassociate = disassociateHolder;

                                        setDisassociating(true);
                                        setDisassociateError("");

                                        try {
                                            const result = await disassociateFgiHolder(
                                                holderToDisassociate.Holder,
                                                user.token
                                            );

                                            setRows((current) =>
                                                current.map((row) =>
                                                    row.Holder === holderToDisassociate.Holder
                                                        ? { ...row, Status: "" }
                                                        : row
                                                )
                                            );
                                            setDisassociateHolder(null);
                                            setSuccessMessage(result.Message || "Holder disassociated successfully.");
                                            setShowSuccess(true);
                                            onDisassociateSuccess?.();
                                        } catch (err) {
                                            setDisassociateError(
                                                err instanceof Error ? err.message : "Disassociate failed."
                                            );
                                        } finally {
                                            setDisassociating(false);
                                        }
                                    }}
                                >
                                    {disassociating ? "Processing..." : "Yes"}
                                </button>
                            </div>
                        </div>
                    </div>
                </div>
            )}

            {showSuccess && (
                <div
                    className="modal d-block"
                    role="dialog"
                    aria-modal="true"
                    style={{ background: "rgba(9, 30, 66, 0.55)", zIndex: 1080 }}
                    onMouseDown={() => setShowSuccess(false)}
                >
                    <div
                        className="modal-dialog modal-dialog-centered"
                        onMouseDown={(event) => event.stopPropagation()}
                    >
                        <div className="modal-content">
                            <div className="modal-header">
                                <h5 className="modal-title">Success</h5>
                                <button
                                    type="button"
                                    className="btn-close"
                                    aria-label="Close"
                                    onClick={() => setShowSuccess(false)}
                                />
                            </div>
                            <div className="modal-body">
                                <p>{successMessage}</p>
                            </div>
                            <div className="modal-footer">
                                <button
                                    className="btn btn-primary"
                                    onClick={() => setShowSuccess(false)}
                                >
                                    OK
                                </button>
                            </div>
                        </div>
                    </div>
                </div>
            )}
        </>
    );
}
