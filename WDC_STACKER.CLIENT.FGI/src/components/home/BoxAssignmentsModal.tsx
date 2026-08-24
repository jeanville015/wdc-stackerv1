import { useEffect, useState } from "react";
import { disassociateFgiHolder, getShipBoxAssignmentsApi } from "../../api/stackerApi";
import { useAuth } from "../../context/useAuth";
import type { BoxAssignment, ShipBoxView } from "../../types/stacker";
import { formatShipBoxName } from "../../utils/nameTransformers";

interface Props {
    boxName: string;
    shipBox: ShipBoxView;
    shipBoxColumnCount?: number;
    boxDisplayName?: string;
    rackDisplayName?: string;
    productName?: string | null;
    partNum?: string | null;
    penNum?: string | null;
    onClose: () => void;
    onDisassociateSuccess?: () => void;
}

export default function BoxAssignmentsModal({
    boxName,
    shipBox,
    shipBoxColumnCount,
    boxDisplayName,
    rackDisplayName,
    productName,
    partNum,
    penNum,
    onClose,
    onDisassociateSuccess,
}: Props) {
    const { user } = useAuth();
    const [rows, setRows] = useState<BoxAssignment[]>([]);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState("");
    const [disassociateHolder, setDisassociateHolder] = useState<BoxAssignment | null>(null);
    const [showSuccess, setShowSuccess] = useState(false);
    const [successMessage, setSuccessMessage] = useState("");
    const [disassociating, setDisassociating] = useState(false);
    const [disassociateError, setDisassociateError] = useState("");

    const shipBoxName = shipBox.ShipBoxName;
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
                return getShipBoxAssignmentsApi(boxName, shipBoxName, token);
            })
            .then((assignments) => {
                if (!isCancelled && assignments) {
                    setRows(assignments);
                }
            })
            .catch((err: unknown) => {
                if (!isCancelled) {
                    setError(err instanceof Error ? err.message : "Load failed.");
                }
            })
            .finally(() => {
                if (!isCancelled) {
                    setLoading(false);
                }
            });

        return () => {
            isCancelled = true;
        };
    }, [boxName, shipBoxName, token]);

    return (
        <>
            <div
                className="modal d-block"
                role="dialog"
                aria-modal="true"
                aria-labelledby="box-assignments-modal-title"
                style={{ background: "rgba(9, 30, 66, 0.55)", zIndex: 1060 }}
                onMouseDown={onClose}
            >
                <div
                    className="modal-dialog modal-xl modal-dialog-centered modal-dialog-scrollable box-assignments-dialog"
                    onMouseDown={(event) => event.stopPropagation()}
                >
                    <div className="modal-content box-assignments-modal">
                        <div className="modal-header box-assignments-modal-header">
                            <div className="shipbox-modal-heading">
                                <span className="shipbox-modal-eyebrow">
                                    Ship Box
                                </span>

                                <h5
                                    id="box-assignments-modal-title"
                                    className="modal-title"
                                >
                                    {formatShipBoxName(shipBox.LayerRowNum, shipBox.LayerColNum, shipBoxColumnCount)}
                                </h5>

                                {(boxDisplayName || rackDisplayName) && (
                                    <div className="shipbox-modal-subtitle">
                                        <i className="fa-solid fa-chevron-right" aria-hidden="true" />
                                        <span>{boxDisplayName}</span>
                                        {rackDisplayName && (
                                            <>
                                                <i className="fa-solid fa-chevron-right" aria-hidden="true" />
                                                <span>{rackDisplayName}</span>
                                            </>
                                        )}
                                    </div>
                                )}

                                <div className="shipbox-modal-metadata">
                                    <span>
                                        <strong>Model:</strong>
                                        {productName ?? "—"}
                                    </span>

                                    <span>
                                        <strong>PartNum:</strong>
                                        {partNum ?? "—"}
                                    </span>

                                    <span>
                                        <strong>PenNum:</strong>
                                        {penNum ?? "—"}
                                    </span>

                                    <span>
                                        <strong>LEC:</strong>
                                        {shipBox.Lec || "—"}
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

                            {displayLoading ? (
                                <div className="text-center p-4">
                                    <span className="spinner-border" />
                                </div>
                            ) : (
                                <div className="table-responsive box-assignments-table-wrap">
                                    <table className="table table-bordered align-middle text-center box-assignments-table">
                                        <thead>
                                            <tr>
                                                <th>Holder</th>
                                                <th>Job</th>
                                                <th>Qty</th>
                                                <th>Factory</th>
                                                <th>LEC</th>
                                                <th>Status</th>
                                                <th>Action</th>
                                            </tr>
                                        </thead>

                                        <tbody>
                                            {rows.map((row) => {
                                                const isHeld =
                                                    (row.Status ?? "").trim().toUpperCase() ===
                                                    "HOLD";

                                                return (
                                                    <tr
                                                        key={row.Holder}
                                                        className={
                                                            isHeld
                                                                ? "box-assignment-row--held"
                                                                : undefined
                                                        }
                                                    >
                                                        <td className="box-assignment-holder">
                                                            {row.Holder}
                                                        </td>

                                                        <td>{row.Job?.trim() || "—"}</td>

                                                        <td>{row.Qty ?? "—"}</td>

                                                        <td>{row.Factory || "—"}</td>

                                                        <td>{row.Lec || "—"}</td>

                                                        <td>
                                                            {isHeld ? (
                                                                <span className="box-assignment-hold-badge">
                                                                    HOLD
                                                                </span>
                                                            ) : (
                                                                <span className="box-assignment-empty-value">
                                                                    —
                                                                </span>
                                                            )}
                                                        </td>

                                                        <td>
                                                            {isHeld ? (
                                                                <button
                                                                    type="button"
                                                                    className="btn btn-outline-danger btn-sm box-assignment-disassociate"
                                                                    aria-label={`Disassociate holder ${row.Holder}`}
                                                                    onClick={() =>
                                                                        setDisassociateHolder(row)
                                                                    }
                                                                >
                                                                    <i
                                                                        className="fa-solid fa-link-slash"
                                                                        aria-hidden="true"
                                                                    />

                                                                    <span>Disassociate</span>
                                                                </button>
                                                            ) : (
                                                                <span className="box-assignment-empty-value">
                                                                    —
                                                                </span>
                                                            )}
                                                        </td>
                                                    </tr>
                                                );
                                            })}

                                            {rows.length === 0 && (
                                                <tr>
                                                    <td
                                                        colSpan={7}
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
