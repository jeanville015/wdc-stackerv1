import { Fragment, useState } from "react";
import type {
    FgiWithdrawalBox,
    FgiWithdrawalShipBox,
} from "../../types/withdrawal";
import {
    formatBoxName,
    formatShipBoxName,
} from "../../utils/nameTransformers";
import WithdrawalHoldersModal from "./WithdrawalHoldersModal";

interface Props {
    box: FgiWithdrawalBox;
    layerCount: number;
    columnCount: number;
    maxItemPerShipBox?: number;
    onClose: () => void;
}

const HOLDER_MATRIX_CAP = 100;
const PROPORTIONAL_SEGMENT_COUNT = 10;

const isInSiteHoldShipBox = (shipBox: FgiWithdrawalShipBox) =>
    shipBox.Holders.some((holder) => Boolean(holder.IsInSiteHold));

const hasHeldHolder = (shipBox: FgiWithdrawalShipBox) =>
    shipBox.Holders.some(
        (holder) =>
            (holder.Status ?? "").trim().toUpperCase() === "HOLD"
    );

const getWithdrawalShipBoxCellClassName = (
    shipBox: FgiWithdrawalShipBox
) =>
    [
        "shipbox-grid-cell",
        "shipbox-grid-cell--mapped",
        (isInSiteHoldShipBox(shipBox) || hasHeldHolder(shipBox)) &&
        "shipbox-grid-cell--held",
    ]
        .filter(Boolean)
        .join(" ");

function WithdrawalShipBoxSegments({
    shipBox,
    maxItems,
}: {
    shipBox: FgiWithdrawalShipBox;
    maxItems?: number;
}) {
    const capacity = Math.max(1, Number(maxItems) || 1);
    const usesHolderMatrix = capacity <= HOLDER_MATRIX_CAP;

    const segmentCount = usesHolderMatrix
        ? capacity
        : PROPORTIONAL_SEGMENT_COUNT;

    const holderCount = Math.max(0, shipBox.Holders.length);

    const occupiedSegments = usesHolderMatrix
        ? Math.min(holderCount, segmentCount)
        : holderCount === 0
            ? 0
            : Math.min(
                segmentCount,
                Math.max(
                    1,
                    Math.round(
                        (holderCount / capacity) * segmentCount
                    )
                )
            );

    const heldPositions = shipBox.Holders.flatMap(
        (holder, index) =>
            holder.IsInSiteHold ||
                (holder.Status ?? "").trim().toUpperCase() === "HOLD"
                ? [index]
                : []
    );

    const heldSegmentIndexes = new Set(
        heldPositions.map((position) =>
            usesHolderMatrix
                ? position
                : Math.min(
                    segmentCount - 1,
                    Math.floor(
                        (position * segmentCount) / capacity
                    )
                )
        )
    );

    return (
        <span className="shipbox-cell-content" aria-hidden="true">
            <span className="shipbox-cell-identity">
                <strong className="shipbox-cell-name-pill">
                    {formatShipBoxName(shipBox.ShipBoxName)}
                </strong>

                <small className="shipbox-cell-count">
                    {holderCount}/{capacity}
                </small>
            </span>

            <span
                className="shipbox-capacity-track"
                style={{
                    gridTemplateColumns:
                        `repeat(${segmentCount}, minmax(0, 1fr))`,
                }}
            >
                {Array.from({ length: segmentCount }, (_, index) => {
                    const isHeld = heldSegmentIndexes.has(index);
                    const isFilled = index < occupiedSegments;

                    const className = [
                        "shipbox-capacity-segment",
                        isHeld
                            ? "is-held"
                            : isFilled
                                ? "is-filled"
                                : "is-available",
                    ].join(" ");

                    return <span key={index} className={className} />;
                })}
            </span>

            <span className="shipbox-cell-action">
                <span>View holders</span>
                <i
                    className="fa-solid fa-chevron-right shipbox-cell-chevron"
                    aria-hidden="true"
                />
            </span>
        </span>
    );
} 

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
                className="modal-dialog modal-xl modal-dialog-centered modal-dialog-scrollable shipbox-grid-dialog"
                onMouseDown={(event) =>
                    event.stopPropagation()
                }
            >
                <div className="modal-content shipbox-grid-modal">
                    <div className="modal-header shipbox-grid-modal-header">
                        <div className="shipbox-modal-heading">
                            <span className="shipbox-modal-eyebrow">
                                Black Box
                            </span>

                            <h5
                                id="withdrawal-shipbox-modal-title"
                                className="modal-title"
                            >
                                {formatBoxName(box.BoxNo, box.LayerColNum)}
                            </h5>

                            <div className="shipbox-modal-metadata">
                                <span>
                                    <strong>Grade (BinName):</strong>
                                    {box.Grade}
                                </span>

                                <span>
                                    <strong>PartNum:</strong>
                                    {box.PartNum}
                                </span>

                                <span>
                                    <strong>PenNum:</strong>
                                    {box.PenNum}
                                </span>

                                <span>
                                    <strong>Capacity:</strong>
                                    {typeof maxItemPerShipBox === "number"
                                        ? `${Math.max(1, maxItemPerShipBox)} holders each`
                                        : "—"}
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
                        <div className="shipbox-grid-layout">
                            <div className="shipbox-grid-scroll">
                                <div className="shipbox-grid">
                                    {layers.map((layerNumber) => (
                                        <Fragment key={`shipbox-layer-${layerNumber}`}>
                                            {columns.map((columnNumber) => {
                                                const shipBox = findShipBox(
                                                    layerNumber,
                                                    columnNumber
                                                );

                                                if (!shipBox) {
                                                    return (
                                                        <button
                                                            type="button"
                                                            key={`empty-shipbox-${layerNumber}-${columnNumber}`}
                                                            className="shipbox-grid-cell shipbox-grid-cell--empty"
                                                            disabled
                                                            aria-label="Empty ShipBox position"
                                                        >
                                                            <span
                                                                className="shipbox-empty-state"
                                                                aria-hidden="true"
                                                            >
                                                                <span>Empty</span>
                                                            </span>
                                                        </button>
                                                    );
                                                }

                                                const inSiteHoldHolders =
                                                    shipBox.Holders
                                                        .filter(
                                                            (holder) =>
                                                                holder.IsInSiteHold
                                                        )
                                                        .map(
                                                            (holder) =>
                                                                holder.Holder
                                                        );

                                                const hasInSiteHold =
                                                    inSiteHoldHolders.length > 0;

                                                return (
                                                    <button
                                                        type="button"
                                                        key={`${shipBox.ShipBoxName}-${shipBox.ShipBoxNum}`}
                                                        className={getWithdrawalShipBoxCellClassName(
                                                            shipBox
                                                        )}
                                                        onClick={() =>
                                                            setSelectedShipBox(shipBox)
                                                        }
                                                        aria-label={`Open holders for ${formatShipBoxName(
                                                            shipBox.ShipBoxName
                                                        )}${hasInSiteHold
                                                                ? ", in-site hold"
                                                                : ""
                                                            }`}
                                                    >
                                                        <WithdrawalShipBoxSegments
                                                            shipBox={shipBox}
                                                            maxItems={maxItemPerShipBox}
                                                        />

                                                        {hasInSiteHold && (
                                                            <span
                                                                className="rack-box-in-site-hold-badge"
                                                                title={`In-site hold: ${inSiteHoldHolders.join(
                                                                    ", "
                                                                )}`}
                                                            >
                                                                IN-SITE
                                                            </span>
                                                        )}
                                                    </button>
                                                );
                                            })}
                                        </Fragment>
                                    ))}
                                </div>
                            </div>

                            <div
                                className="shipbox-status-legend"
                                aria-label="Ship box status legend"
                            >
                                <span>
                                    <i
                                        className="shipbox-legend-swatch is-available"
                                        aria-hidden="true"
                                    />
                                    Available
                                </span>

                                <span>
                                    <i
                                        className="shipbox-legend-swatch is-occupied"
                                        aria-hidden="true"
                                    />
                                    Occupied
                                </span>

                                <span>
                                    <i
                                        className="shipbox-legend-swatch is-held"
                                        aria-hidden="true"
                                    />
                                    Hold
                                </span>
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