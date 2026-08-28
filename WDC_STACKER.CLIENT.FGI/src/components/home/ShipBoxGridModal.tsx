import { Fragment, useEffect, useState } from "react";
import { getShipBoxesApi } from "../../api/stackerApi";
import { useAuth } from "../../context/useAuth";
import type { BoxView, ShipBoxView } from "../../types/stacker";
import { formatBoxName, formatRackName, formatShipBoxName } from "../../utils/nameTransformers";
import BoxAssignmentsModal from "./BoxAssignmentsModal";
import { getCornerHighlightStyle } from "./rackGridStyles";

interface Props {
    box: BoxView;
    layerCount: number;
    boxCount: number;
    rackBoxColumnCount: number;
    maxItemPerShipBox: number;
    shipBoxSelectionEnabled: boolean;
    selectedTargetShipBox: ShipBoxView | null;
    onTargetShipBoxSelected: (box: BoxView, shipBox: ShipBoxView) => void;
    onClose: () => void;
    onDisassociateSuccess?: () => void;
}

const BLUE_BORDER = "#003d99";
const HOLDER_MATRIX_CAP = 100;
const PROPORTIONAL_SEGMENT_COUNT = 10;

const isReleaseShipBox = (shipBox: ShipBoxView) =>
    shipBox.HasReleaseStatus ||
    shipBox.ShipBoxStatus.trim().toUpperCase() === "RELEASE";

const isInSiteHoldShipBox = (shipBox: ShipBoxView) =>
    Boolean(
        shipBox.HasInSiteHold ||
        (shipBox.InSiteHoldHolders?.length ?? 0) > 0
    );

const getShipBoxCellClassName = (
    shipBox: ShipBoxView,
    isHighlighted: boolean
) =>
    [
        "shipbox-grid-cell",
        "shipbox-grid-cell--mapped",
        isReleaseShipBox(shipBox) && "shipbox-grid-cell--release",
        (isInSiteHoldShipBox(shipBox) || shipBox.HasHeldHolder) &&
        "shipbox-grid-cell--held",
        isHighlighted && "shipbox-grid-cell--highlighted",
    ]
        .filter(Boolean)
        .join(" ");

function ShipBoxSegments({
    shipBox,
    maxItems,
    shipBoxColumnCount,
}: {
    shipBox: ShipBoxView;
    maxItems: number;
    shipBoxColumnCount: number;
}) {
    const capacity = Math.max(1, Number(maxItems) || 1);
    const usesHolderMatrix = capacity <= HOLDER_MATRIX_CAP;
    const segmentCount = usesHolderMatrix
        ? capacity
        : PROPORTIONAL_SEGMENT_COUNT;

    const itemCount = Math.max(
        0,
        Number(shipBox.ShipBoxListCount) || 0
    );

    const occupiedSegments = usesHolderMatrix
        ? Math.min(itemCount, segmentCount)
        : itemCount === 0
            ? 0
            : Math.min(
                segmentCount,
                Math.max(
                    1,
                    Math.round(
                        (itemCount / capacity) * segmentCount
                    )
                )
            );

    const heldPositions = Array.from(
        new Set([
            ...(shipBox.InSiteHoldPositions ?? []),
            ...(shipBox.HeldHolderPositions ?? []),
        ])
    ).filter(
        (position) =>
            Number.isInteger(position) &&
            position >= 0 &&
            position < itemCount
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

    const isRelease = isReleaseShipBox(shipBox); 

    return (
        <span
            className="shipbox-cell-content"
            aria-hidden="true"
        >
            <span className="shipbox-cell-identity">
                <strong className="shipbox-cell-name-pill">
                    {formatShipBoxName(shipBox.LayerRowNum, shipBox.LayerColNum, shipBoxColumnCount)}
                </strong>

                <small className="shipbox-cell-count">
                    {itemCount}/{capacity}
                </small>
            </span>

            <span
                className="shipbox-capacity-track"
                style={{
                    gridTemplateColumns: `repeat(${segmentCount}, minmax(0, 1fr))`,
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
                        isRelease && "is-release",
                    ]
                        .filter(Boolean)
                        .join(" ");

                    return (
                        <span
                            key={index}
                            className={className}
                        />
                    );
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

export default function ShipBoxGridModal({
    box,
    layerCount,
    boxCount,
    rackBoxColumnCount,
    maxItemPerShipBox,
    shipBoxSelectionEnabled,
    selectedTargetShipBox,
    onTargetShipBoxSelected,
    onClose,
    onDisassociateSuccess,
}: Props) {
    const { user } = useAuth();
    const [shipBoxes, setShipBoxes] = useState<ShipBoxView[]>(box.ShipBoxes ?? []);
    const [selectedShipBox, setSelectedShipBox] = useState<ShipBoxView | null>(null);
    const [loading, setLoading] = useState(false);
    const [error, setError] = useState("");
    const token = user?.token;

    const columns = Array.from({ length: Math.max(0, boxCount) }, (_, index) => index + 1);
    const layers = Array.from({ length: Math.max(0, layerCount) }, (_, index) => index + 1);

    useEffect(() => {
        if (!token) return;

        let isCancelled = false;

        Promise.resolve()
            .then(() => {
                if (isCancelled) return undefined;

                setLoading(true);
                setError("");
                return getShipBoxesApi(box.BoxNo, token, shipBoxSelectionEnabled);
            })
            .then((result) => {
                if (!isCancelled && result) {
                    setShipBoxes(result);
                }
            })
            .catch((err: unknown) => {
                if (!isCancelled) {
                    setError(err instanceof Error ? err.message : "Unable to load ShipBoxes.");
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
    }, [box.BoxNo, shipBoxSelectionEnabled, token]);

    const findShipBox = (layerNumber: number, columnNumber: number) => {
        return shipBoxes.find(
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
            style={{ background: "rgba(9, 30, 66, 0.55)" }}
            onMouseDown={onClose}
        >
            <div
                className="modal-dialog modal-xl modal-dialog-centered modal-dialog-scrollable shipbox-grid-dialog"
                onMouseDown={(event) => event.stopPropagation()}
            >
                <div className="modal-content shipbox-grid-modal">
                    <div className="modal-header shipbox-grid-modal-header">
                        <div className="shipbox-modal-heading">
                            <span className="shipbox-modal-eyebrow">
                                Black Box
                            </span>

                            <h5 className="modal-title">
                                {formatBoxName(box.LayerRowNum, box.LayerColNum, rackBoxColumnCount)}
                            </h5>

                            <div className="shipbox-modal-subtitle">
                                <i className="fa-solid fa-chevron-right" aria-hidden="true" />
                                <span>{formatRackName(box.RackNum)}</span>
                            </div>

                            <div className="shipbox-modal-metadata">
                                <span>
                                    <strong>Model:</strong>
                                    {box.ProductName}
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
                                    <strong>CAM:</strong>
                                    {box.CamVersion ? `${box.CamVersion}` : "—"}
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
                        {error && <div className="alert alert-danger">{error}</div>}

                        {loading ? (
                            <div className="text-center p-4">
                                <span className="spinner-border" />
                            </div>
                        ) : (
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

                                                    const isHighlighted =
                                                        selectedTargetShipBox?.BoxNo ===
                                                        box.BoxNo &&
                                                        selectedTargetShipBox?.ShipBoxName ===
                                                        shipBox?.ShipBoxName;

                                                    return (
                                                        <button
                                                            type="button"
                                                            key={`shipbox-layer-${layerNumber}-column-${columnNumber}`}
                                                            className={
                                                                shipBox
                                                                    ? getShipBoxCellClassName(
                                                                        shipBox,
                                                                        isHighlighted
                                                                    )
                                                                    : "shipbox-grid-cell shipbox-grid-cell--empty"
                                                            }
                                                            disabled={!shipBox}
                                                            onClick={() => {
                                                                if (!shipBox) return;

                                                                if (shipBoxSelectionEnabled) {
                                                                    onTargetShipBoxSelected(
                                                                        box,
                                                                        shipBox
                                                                    );
                                                                    onClose();
                                                                    return;
                                                                }

                                                                setSelectedShipBox(shipBox);
                                                            }}
                                                            aria-label={
                                                                shipBox
                                                                    ? `View holders in ${formatShipBoxName(
                                                                        shipBox.LayerRowNum,
                                                                        shipBox.LayerColNum,
                                                                        boxCount
                                                                    )}`
                                                                    : "Empty ShipBox position"
                                                            }
                                                        >
                                                            {shipBox ? (
                                                                <>
                                                                    <ShipBoxSegments
                                                                        shipBox={shipBox}
                                                                        maxItems={maxItemPerShipBox}
                                                                        shipBoxColumnCount={boxCount}
                                                                    />

                                                                    {isInSiteHoldShipBox(shipBox) && (
                                                                        <span
                                                                            className="rack-box-in-site-hold-badge"
                                                                            title={`In-site hold: ${(
                                                                                shipBox.InSiteHoldHolders ?? []
                                                                            ).join(", ")}`}
                                                                        >
                                                                            IN-SITE
                                                                        </span>
                                                                    )}

                                                                    {isHighlighted && (
                                                                        <>
                                                                            <span
                                                                                style={getCornerHighlightStyle(
                                                                                    "topLeft",
                                                                                    BLUE_BORDER
                                                                                )}
                                                                            />
                                                                            <span
                                                                                style={getCornerHighlightStyle(
                                                                                    "topRight",
                                                                                    BLUE_BORDER
                                                                                )}
                                                                            />
                                                                            <span
                                                                                style={getCornerHighlightStyle(
                                                                                    "bottomLeft",
                                                                                    BLUE_BORDER
                                                                                )}
                                                                            />
                                                                            <span
                                                                                style={getCornerHighlightStyle(
                                                                                    "bottomRight",
                                                                                    BLUE_BORDER
                                                                                )}
                                                                            />
                                                                        </>
                                                                    )}
                                                                </>
                                                            ) : (
                                                                <span className="shipbox-empty-state" aria-hidden="true">
                                                                    <span>Empty</span>
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
                        )}

                        {selectedShipBox && (
                            <BoxAssignmentsModal
                                boxName={box.BoxNo}
                                shipBox={selectedShipBox}
                                shipBoxColumnCount={boxCount}
                                boxDisplayName={formatBoxName(box.LayerRowNum, box.LayerColNum, rackBoxColumnCount)}
                                rackDisplayName={formatRackName(box.RackNum)}
                                productName={box.ProductName}
                                partNum={box.PartNum}
                                penNum={box.PenNum}
                                camVersion={box.CamVersion}
                                onClose={() => setSelectedShipBox(null)}
                                onDisassociateSuccess={onDisassociateSuccess}
                            />
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
