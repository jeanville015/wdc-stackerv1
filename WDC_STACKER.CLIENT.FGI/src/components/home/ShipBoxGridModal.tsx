import { Fragment, useEffect, useState, type CSSProperties } from "react";
import { getShipBoxesApi } from "../../api/stackerApi";
import { useAuth } from "../../context/AuthContext";
import type { BoxView, ShipBoxView } from "../../types/stacker";
import BoxAssignmentsModal from "./BoxAssignmentsModal";
import {
    columnLabelCellStyle,
    cornerCellStyle,
    getCornerHighlightStyle,
    getEmptyCellStyle,
    rackGridStyle,
    rackScrollStyle,
    rowLabelCellStyle,
} from "./rackGridStyles";

interface Props {
    box: BoxView;
    layerCount: number;
    boxCount: number;
    maxItemPerShipBox: number;
    shipBoxSelectionEnabled: boolean;
    selectedTargetShipBox: ShipBoxView | null;
    onTargetShipBoxSelected: (box: BoxView, shipBox: ShipBoxView) => void;
    onClose: () => void;
    onDisassociateSuccess?: () => void;
}

const BLUE_DARK = "#0052cc";
const BLUE_LIGHT = "#cfe3ff";
const BLUE_BORDER = "#003d99";
const GREEN_DARK = "#16833a";
const GREEN_LIGHT = "#d8f3df";
const SEGMENT_CAP = 10;

const isReleaseShipBox = (shipBox: ShipBoxView) =>
    shipBox.HasReleaseStatus || shipBox.ShipBoxStatus.trim().toUpperCase() === "RELEASE";

const isInSiteHoldShipBox = (shipBox: ShipBoxView) =>
    Boolean(shipBox.HasInSiteHold || (shipBox.InSiteHoldHolders?.length ?? 0) > 0);

const getShipBoxCellStyle = (
    shipBox: ShipBoxView,
    isHighlighted: boolean
): CSSProperties => {
    const isInSiteHold = isInSiteHoldShipBox(shipBox);

    return {
        ...getEmptyCellStyle(),
        background: isInSiteHold || shipBox.HasHeldHolder
            ? "#ff4d4d"
            : isReleaseShipBox(shipBox) ? GREEN_LIGHT : BLUE_LIGHT,
        border: isInSiteHold ? "1px solid #a51f1f" : "1px solid #8bbcff",
        padding: "0.25rem",
        position: "relative",
        cursor: "pointer",
        overflow: isHighlighted ? "visible" : "hidden",
    };
};

function ShipBoxSegments({ shipBox, maxItems }: { shipBox: ShipBoxView; maxItems: number }) {
    const configuredCapacity = Math.max(1, Number(maxItems) || 1);
    const visibleCapacity = Math.min(configuredCapacity, SEGMENT_CAP);
    const itemCount = Math.max(0, Number(shipBox.ShipBoxListCount) || 0);
    const occupiedSegments = itemCount === 0
        ? 0
        : Math.min(
            visibleCapacity,
            Math.max(1, Math.round((itemCount / configuredCapacity) * visibleCapacity))
        );
    const isRelease = isReleaseShipBox(shipBox);
    const filledColor = isRelease ? GREEN_DARK : BLUE_DARK;
    const availableColor = isRelease ? GREEN_LIGHT : BLUE_LIGHT;

    return (
        <span
            aria-hidden="true"
            style={{
                position: "absolute",
                inset: 0,
                overflow: "hidden",
                pointerEvents: "none",
            }}
        >
            <span
                style={{
                    position: "absolute",
                    inset: "4px",
                    display: "grid",
                    gridTemplateColumns: `repeat(${visibleCapacity}, minmax(0, 1fr))`,
                    gap: "2px",
                    padding: "3px",
                    borderRadius: "6px",
                    background: availableColor,
                }}
            >
                {Array.from({ length: visibleCapacity }, (_, index) => (
                    <span
                        key={index}
                        style={{
                            minWidth: 0,
                            borderRadius: "3px",
                            background: index < occupiedSegments ? filledColor : availableColor,
                            boxShadow: "inset 0 1px 0 rgba(255,255,255,0.32)",
                        }}
                    />
                ))}
            </span>
            <span
                style={{
                    position: "absolute",
                    inset: 0,
                    display: "flex",
                    flexDirection: "column",
                    alignItems: "center",
                    justifyContent: "center",
                    gap: "1px",
                    padding: "0.2rem",
                    color: occupiedSegments > 0 ? "#ffffff" : "#172b4d",
                    fontSize: "0.66rem",
                    fontWeight: 800,
                    lineHeight: 1.05,
                    textAlign: "center",
                    textShadow: occupiedSegments > 0 ? "0 1px 2px rgba(0,0,0,0.55)" : undefined,
                    overflowWrap: "anywhere",
                }}
            >
                <span>{shipBox.ShipBoxName}</span>
                <small>{itemCount}/{configuredCapacity}</small>
            </span>
        </span>
    );
}

export default function ShipBoxGridModal({
    box,
    layerCount,
    boxCount,
    maxItemPerShipBox,
    shipBoxSelectionEnabled,
    selectedTargetShipBox,
    onTargetShipBoxSelected,
    onClose,
    onDisassociateSuccess,
}: Props) {
    const { user } = useAuth();
    const hasToken = Boolean(user?.token);
    const [shipBoxes, setShipBoxes] = useState<ShipBoxView[]>(box.ShipBoxes ?? []);
    const [selectedShipBox, setSelectedShipBox] = useState<ShipBoxView | null>(null);
    const [loading, setLoading] = useState(hasToken);
    const [error, setError] = useState("");
    const displayError = error || (!hasToken ? "Login token is missing." : "");

    const columns = Array.from({ length: Math.max(0, boxCount) }, (_, index) => index + 1);
    const layers = Array.from({ length: Math.max(0, layerCount) }, (_, index) => index + 1);

    useEffect(() => {
        if (!user?.token) return;

        getShipBoxesApi(box.BoxNo, user.token, shipBoxSelectionEnabled)
            .then((result) => {
                setError("");
                setShipBoxes(result);
            })
            .catch((err: unknown) =>
                setError(err instanceof Error ? err.message : "Unable to load ShipBoxes.")
            )
            .finally(() => setLoading(false));
    }, [box.BoxNo, shipBoxSelectionEnabled, user?.token]);

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
                className="modal-dialog modal-xl modal-dialog-centered modal-dialog-scrollable"
                onMouseDown={(event) => event.stopPropagation()}
            >
                <div className="modal-content">
                    <div className="modal-header">
                        <div className="stacker-modal-header-info">
                            <h5 className="modal-title">Black Box: {box.BoxNo}</h5>

                            <div className="stacker-detail-pills">
                                <span className="stacker-detail-pill">
                                    <strong>Model (ProductName):</strong> {box.ProductName}
                                </span>
                                <span className="stacker-detail-pill">
                                    <strong>PartNum:</strong> {box.PartNum}
                                </span>
                                <span className="stacker-detail-pill">
                                    <strong>PenNum:</strong> {box.PenNum}
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
                            <div style={rackScrollStyle}>
                                <div style={rackGridStyle(columns.length, layers.length)}>
                                    <div style={cornerCellStyle} />

                                    {columns.map((columnNumber) => (
                                        <div
                                            key={`shipbox-column-${columnNumber}`}
                                            style={columnLabelCellStyle}
                                        >
                                            {columnNumber}
                                        </div>
                                    ))}

                                    {layers.map((layerNumber) => (
                                        <Fragment key={`shipbox-layer-${layerNumber}`}>
                                            <div style={rowLabelCellStyle}>Layer {layerNumber}</div>

                                            {columns.map((columnNumber) => {
                                                const shipBox = findShipBox(layerNumber, columnNumber);
                                                const isHighlighted =
                                                    selectedTargetShipBox?.BoxNo === box.BoxNo &&
                                                    selectedTargetShipBox?.ShipBoxName === shipBox?.ShipBoxName;
                                                return (
                                                    <button
                                                        type="button"
                                                        key={`shipbox-layer-${layerNumber}-column-${columnNumber}`}
                                                        disabled={!shipBox}
                                                        onClick={() => {
                                                            if (!shipBox) return;

                                                            if (shipBoxSelectionEnabled) {
                                                                onTargetShipBoxSelected(box, shipBox);
                                                                onClose();
                                                                return;
                                                            }

                                                            setSelectedShipBox(shipBox);
                                                        }}
                                                        style={
                                                            shipBox
                                                                ? getShipBoxCellStyle(shipBox, isHighlighted)
                                                                : getEmptyCellStyle()
                                                        }
                                                        aria-label={
                                                            shipBox
                                                                ? `Open holder assignments for ${shipBox.ShipBoxName}`
                                                                : "Empty ShipBox cell"
                                                        }
                                                    >
                                                        {shipBox && (
                                                            <>
                                                                <ShipBoxSegments
                                                                    shipBox={shipBox}
                                                                    maxItems={maxItemPerShipBox}
                                                                />

                                                                {isInSiteHoldShipBox(shipBox) && (
                                                                    <span
                                                                        className="rack-box-in-site-hold-badge"
                                                                        title={`In-site hold: ${(shipBox.InSiteHoldHolders ?? []).join(", ")}`}
                                                                    >
                                                                        IN-SITE
                                                                    </span>
                                                                )}

                                                                {isHighlighted && (
                                                                    <>
                                                                        <span style={getCornerHighlightStyle("topLeft", BLUE_BORDER)} />
                                                                        <span style={getCornerHighlightStyle("topRight", BLUE_BORDER)} />
                                                                        <span style={getCornerHighlightStyle("bottomLeft", BLUE_BORDER)} />
                                                                        <span style={getCornerHighlightStyle("bottomRight", BLUE_BORDER)} />
                                                                    </>
                                                                )}
                                                            </>
                                                        )}
                                                    </button>
                                                );
                                            })}
                                        </Fragment>
                                    ))}
                                </div>
                            </div>
                        )}

                        {selectedShipBox && (
                            <BoxAssignmentsModal
                                boxName={box.BoxNo}
                                shipBox={selectedShipBox}
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
