import {
    Fragment,
    useState,
    type CSSProperties,
} from "react";
import type {
    FgiWithdrawalBox,
    FgiWithdrawalShipBox,
} from "../../types/withdrawal";
import {
    columnLabelCellStyle,
    cornerCellStyle,
    getEmptyCellStyle,
    rackGridStyle,
    rackScrollStyle,
    rowLabelCellStyle,
} from "../home/rackGridStyles";
import WithdrawalHoldersModal from "./WithdrawalHoldersModal";

interface Props {
    box: FgiWithdrawalBox;
    layerCount: number;
    columnCount: number;
    maxItemPerShipBox?: number;
    onClose: () => void;
}

const mappedShipBoxStyle: CSSProperties = {
    ...getEmptyCellStyle(),
    position: "relative",
    background: "#cfe3ff",
    border: "1px solid #8bbcff",
    padding: "0.35rem",
    color: "#172b4d",
    cursor: "pointer",
    fontSize: "0.72rem",
    fontWeight: 800,
    lineHeight: 1.1,
    textAlign: "center",
    overflowWrap: "anywhere",
};

const heldShipBoxStyle: CSSProperties = {
    ...mappedShipBoxStyle,
    background: "#ff4d4d",
    borderColor: "#ff6b6b",
    color: "#fff",
};

const isInSiteHoldShipBox = (shipBox: FgiWithdrawalShipBox) =>
    shipBox.Holders.some((holder) => holder.IsInSiteHold);

export default function WithdrawalShipBoxModal({
    box,
    layerCount,
    columnCount,
    maxItemPerShipBox,
    onClose,
}: Props) {
    const [selectedShipBox, setSelectedShipBox] =
        useState<FgiWithdrawalShipBox | null>(null);

    const layers = Array.from(
        { length: Math.max(0, layerCount) },
        (_, index) => index + 1
    );

    const columns = Array.from(
        { length: Math.max(0, columnCount) },
        (_, index) => index + 1
    );

    const findShipBox = (
        layerNumber: number,
        columnNumber: number
    ): FgiWithdrawalShipBox | undefined => {
        return box.ShipBoxes.find(
            (shipBox) =>
                shipBox.LayerRowNum === layerNumber &&
                shipBox.LayerColNum === columnNumber
        );
    };

    return (
        <div
            className="modal d-block"
            role="dialog"
            aria-modal="true"
            aria-labelledby="withdrawal-shipbox-modal-title"
            style={{ background: "rgba(9, 30, 66, 0.55)" }}
            onMouseDown={onClose}
        >
            <div
                className="modal-dialog modal-xl modal-dialog-centered modal-dialog-scrollable"
                onMouseDown={(event) =>
                    event.stopPropagation()
                }
            >
                <div className="modal-content">
                    <div className="modal-header">
                        <div className="stacker-modal-header-info">
                            <h5
                                id="withdrawal-shipbox-modal-title"
                                className="modal-title"
                            >
                                Black Box: {box.BoxNo}
                            </h5>

                            <div className="stacker-detail-pills">
                                <span className="stacker-detail-pill">
                                    <strong>Grade (BinName):</strong> {box.Grade}
                                </span>
                                <span className="stacker-detail-pill">
                                    <strong>PartNum:</strong> {box.PartNum}
                                </span>
                                <span className="stacker-detail-pill">
                                    <strong>PenNum:</strong> {box.PenNum}
                                </span>
                                {typeof maxItemPerShipBox === "number" && (
                                    <span className="stacker-detail-pill">
                                        <strong>Capacity:</strong> {Math.max(1, maxItemPerShipBox)} holders each
                                    </span>
                                )}
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
                        <div style={rackScrollStyle}>
                            <div
                                style={rackGridStyle(
                                    columns.length,
                                    layers.length
                                )}
                            >
                                <div style={cornerCellStyle} />

                                {columns.map(
                                    (columnNumber) => (
                                        <div
                                            key={`shipbox-column-${columnNumber}`}
                                            style={
                                                columnLabelCellStyle
                                            }
                                        >
                                            {columnNumber}
                                        </div>
                                    )
                                )}

                                {layers.map(
                                    (layerNumber) => (
                                        <Fragment
                                            key={`shipbox-layer-${layerNumber}`}
                                        >
                                            <div
                                                style={
                                                    rowLabelCellStyle
                                                }
                                            >
                                                Layer{" "}
                                                {layerNumber}
                                            </div>

                                            {columns.map(
                                                (
                                                    columnNumber
                                                ) => {
                                                    const shipBox =
                                                        findShipBox(
                                                            layerNumber,
                                                            columnNumber
                                                        );

                                                    if (
                                                        !shipBox
                                                    ) {
                                                        return (
                                                            <button
                                                                type="button"
                                                                key={[
                                                                    "empty-shipbox",
                                                                    layerNumber,
                                                                    columnNumber,
                                                                ].join(
                                                                    "-"
                                                                )}
                                                                disabled
                                                                style={{
                                                                    ...getEmptyCellStyle(),
                                                                    cursor: "default",
                                                                }}
                                                                aria-label="Empty ShipBox cell"
                                                            />
                                                        );
                                                    }

                                                    const hasHeldHolder = shipBox.Holders.some(
                                                        (holder) => holder.Status === "HOLD"
                                                    );
                                                    const hasInSiteHold = isInSiteHoldShipBox(shipBox);

                                                    return (
                                                        <button
                                                            type="button"
                                                            key={[
                                                                shipBox.ShipBoxName,
                                                                shipBox.ShipBoxNum,
                                                            ].join(
                                                                "-"
                                                            )}
                                                            className="withdrawal-shipbox-cell"
                                                            style={
                                                                hasInSiteHold || hasHeldHolder
                                                                    ? heldShipBoxStyle
                                                                    : mappedShipBoxStyle
                                                            }
                                                            onClick={() =>
                                                                setSelectedShipBox(
                                                                    shipBox
                                                                )
                                                            }
                                                            aria-label={`Open holders for ${shipBox.ShipBoxName}${hasInSiteHold ? ", in-site hold" : ""}`}
                                                        >
                                                            {
                                                                shipBox.ShipBoxName
                                                            }

                                                            {hasInSiteHold && (
                                                                <span
                                                                    className="rack-box-in-site-hold-badge"
                                                                >
                                                                    IN-SITE
                                                                </span>
                                                            )}
                                                        </button>
                                                    );
                                                }
                                            )}
                                        </Fragment>
                                    )
                                )}
                            </div>
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

            {selectedShipBox && (
                <WithdrawalHoldersModal
                    shipBox={selectedShipBox}
                    onClose={() =>
                        setSelectedShipBox(null)
                    }
                />
            )}
        </div>
    );
}