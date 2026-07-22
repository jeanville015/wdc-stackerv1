import { useEffect, useState } from "react";
import { getShipBoxAssignmentsApi } from "../../api/stackerApi";
import { useAuth } from "../../context/AuthContext";
import type { BoxAssignment } from "../../types/stacker";

interface Props {
    boxName: string;
    shipBoxName: string;
    onClose: () => void;
}

export default function BoxAssignmentsModal({
    boxName,
    shipBoxName,
    onClose,
}: Props) {
    const { user } = useAuth();
    const [rows, setRows] = useState<BoxAssignment[]>([]);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState("");

    useEffect(() => {
        if (!user?.token) {
            setError("Login token is missing.");
            setLoading(false);
            return;
        }

        setLoading(true);
        getShipBoxAssignmentsApi(boxName, shipBoxName, user.token)
            .then(setRows)
            .catch((err: unknown) =>
                setError(err instanceof Error ? err.message : "Load failed.")
            )
            .finally(() => setLoading(false));
    }, [shipBoxName, user?.token]);

    return (
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
                        <h5 className="modal-title">
                            Holder Assignments: {shipBoxName}
                        </h5>
                        <button
                            type="button"
                            className="btn-close"
                            aria-label="Close"
                            onClick={onClose}
                        />
                    </div>

                    <div className="modal-body">
                        {error && <div className="alert alert-danger">{error}</div>}

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
                                        </tr>
                                    </thead>
                                    <tbody>
                                        {rows.map((row) => (
                                            <tr key={row.Holder}>
                                                <td>{row.Holder}</td>
                                                <td>{row.ProductName}</td>
                                                <td>{row.Factory}</td>
                                                <td>{row.Lec}</td>
                                                <td>{row.Partnum}</td>
                                                <td>{row.Pennum}</td>
                                                <td>{row.Status}</td>
                                            </tr>
                                        ))}

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
    );
}
