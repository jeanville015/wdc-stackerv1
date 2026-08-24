import type {
    MouseEvent,
} from "react";
import type {
    FgiWithdrawalShipBox,
} from "../../types/withdrawal";
import { formatShipBoxName } from "../../utils/nameTransformers";

interface Props {
    shipBox: FgiWithdrawalShipBox;
    shipBoxColumnCount?: number;
    boxDisplayName?: string;
    rackDisplayName?: string;
    onClose: () => void;
}

export default function WithdrawalHoldersModal({
    shipBox,
    shipBoxColumnCount,
    boxDisplayName,
    rackDisplayName,
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
                className="modal-dialog modal-xl modal-dialog-centered modal-dialog-scrollable box-assignments-dialog"
                onMouseDown={(event) =>
                    event.stopPropagation()
                }
            >
                <div className="modal-content box-assignments-modal">
                    <div className="modal-header box-assignments-modal-header">
                        <div className="shipbox-modal-heading">
                            <span className="shipbox-modal-eyebrow">
                                Ship Box
                            </span>

                            <h5
                                id="withdrawal-holders-modal-title"
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
                        <div className="table-responsive box-assignments-table-wrap">
                            <table className="table table-bordered align-middle text-center box-assignments-table">
                                <thead>
                                    <tr>
                                        <th scope="col">Holder</th>
                                        <th scope="col">Product Name</th>
                                        <th scope="col">Factory</th>
                                        <th scope="col">LEC</th>
                                        <th scope="col">Status</th>
                                        <th scope="col">Qty</th>
                                    </tr>
                                </thead>

                                <tbody>
                                    {shipBox.Holders.map((holder) => {
                                        const status = (holder.Status ?? "").trim();
                                        const isHeld =
                                            status.toUpperCase() === "HOLD";
                                        const isInSiteHold =
                                            Boolean(holder.IsInSiteHold);

                                        return (
                                            <tr
                                                key={[holder.Holder, holder.Qty].join("-")}
                                                className={
                                                    isHeld || isInSiteHold
                                                        ? "box-assignment-row--held"
                                                        : undefined
                                                }
                                            >
                                                <td className="box-assignment-holder">
                                                    {holder.Holder}
                                                </td>

                                                <td>{holder.ProductName || "—"}</td>
                                                <td>{holder.Factory || "—"}</td>
                                                <td>{shipBox.Lec || "—"}</td>

                                                <td>
                                                    <span className="d-inline-flex flex-wrap align-items-center justify-content-center gap-2">
                                                        {isHeld ? (
                                                            <span className="box-assignment-hold-badge">
                                                                HOLD
                                                            </span>
                                                        ) : status ? (
                                                            <span>{status}</span>
                                                        ) : !isInSiteHold ? (
                                                            <span className="box-assignment-empty-value">
                                                                —
                                                            </span>
                                                        ) : null}

                                                        {isInSiteHold && (
                                                            <span
                                                                className="rack-box-in-site-hold-badge"
                                                                style={{ position: "static" }}
                                                            >
                                                                IN-SITE
                                                            </span>
                                                        )}
                                                    </span>
                                                </td>

                                                <td>{holder.Qty ?? "—"}</td>
                                            </tr>
                                        );
                                    })}

                                    {shipBox.Holders.length === 0 && (
                                        <tr>
                                            <td
                                                colSpan={6}
                                                className="text-center text-muted p-4"
                                            >
                                                No holder records were found.
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