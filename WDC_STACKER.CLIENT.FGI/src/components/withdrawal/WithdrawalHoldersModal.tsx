import type {
    MouseEvent,
} from "react";
import type {
    FgiWithdrawalShipBox,
} from "../../types/withdrawal";

interface Props {
    shipBox: FgiWithdrawalShipBox;
    onClose: () => void;
}

export default function WithdrawalHoldersModal({
    shipBox,
    onClose,
}: Props) {
    const handleBackdropMouseDown = (
        event: MouseEvent<HTMLDivElement>
    ) => {
        /*
         * Prevent the event from reaching the first modal.
         * Otherwise, closing this modal from its backdrop could also
         * close the ShipBox modal.
         */
        event.stopPropagation();
        onClose();
    };

    return (
        <div
            className="modal d-block"
            role="dialog"
            aria-modal="true"
            aria-labelledby="withdrawal-holders-modal-title"
            style={{
                background: "rgba(9, 30, 66, 0.55)",
                zIndex: 1060,
            }}
            onMouseDown={handleBackdropMouseDown}
        >
            <div
                className="modal-dialog modal-lg modal-dialog-centered modal-dialog-scrollable"
                onMouseDown={(event) =>
                    event.stopPropagation()
                }
            >
                <div className="modal-content">
                    <div className="modal-header">
                        <div className="stacker-modal-header-info">
                            <h5
                                id="withdrawal-holders-modal-title"
                                className="modal-title"
                            >
                                Ship Box: {shipBox.ShipBoxName}
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
                        <div className="table-responsive">
                            <table className="table table-bordered table-hover align-middle mb-0">
                                <thead className="table-light">
                                    <tr>
                                        <th scope="col">Holder</th>
                                        <th scope="col">Product Name</th>
                                        <th scope="col">Factory</th>
                                        <th scope="col">LEC</th>
                                        <th scope="col">Status</th>
                                        <th scope="col" className="text-end">
                                            Qty
                                        </th>
                                    </tr>
                                </thead>

                                <tbody>
                                    {shipBox.Holders.map(
                                        (holder) => (
                                            <tr
                                                key={[
                                                    holder.Holder,
                                                    holder.Qty,
                                                ].join("-")}
                                                className={[
                                                    holder.Status === "HOLD" ? "table-danger" : "",
                                                    holder.IsInSiteHold ? "withdrawal-holder-row is-in-site-hold" : "",
                                                ].filter(Boolean).join(" ")}
                                            >
                                                <td>{holder.Holder}</td>
                                                <td>{holder.ProductName}</td>
                                                <td>{holder.Factory}</td>
                                                <td>{shipBox.Lec}</td>
                                                <td>
                                                    {holder.Status}
                                                    {holder.IsInSiteHold && (
                                                        <span className="rack-box-in-site-hold-badge" style={{ position: "static", marginLeft: "0.5rem" }}>
                                                            IN-SITE
                                                        </span>
                                                    )}
                                                </td>
                                                <td className="text-end">
                                                    {holder.Qty}
                                                </td>
                                            </tr>
                                        )
                                    )}

                                    {shipBox.Holders.length ===
                                        0 && (
                                            <tr>
                                                <td
                                                    colSpan={6}
                                                    className="text-center text-muted p-4"
                                                >
                                                    No holder records
                                                    were found.
                                                </td>
                                            </tr>
                                        )}
                                </tbody>
                            </table>
                        </div>
                    </div>

                    <div className="modal-footer">
                        <button
                            type="button"
                            className="btn btn-secondary"
                            onClick={onClose}
                        >
                            Close
                        </button>
                    </div>
                </div>
            </div>
        </div>
    );
}