import { Fragment, useState, type CSSProperties } from "react";
import ShipBoxGridModal from "./ShipBoxGridModal";
import type { BoxView, ShipBoxView } from "../../types/stacker";
import { formatBoxName, formatRackName, formatShipBoxName } from "../../utils/nameTransformers";
import {
    getEmptyCellStyle,
    rackCardStyle,
    rackHeaderStyle,
    rackOverviewGridStyle,
    rackScrollStyle,
    rackTitleStyle,
    rowLabelCellStyle,
} from "./rackGridStyles";

interface RackPanelProps {
    recentlyAssignedBoxNo?: string | null;
    rackNumber: number;
    layerCount: number;
    boxCount: number;
    shipBoxLayerCount: number;
    shipBoxBoxCount: number;
    maxItemPerShipBox: number;
    boxes?: BoxView[];
    boxSelectionEnabled: boolean;
    selectedTargetBox: BoxView | null;
    selectedTargetShipBox: ShipBoxView | null;
    onTargetShipBoxSelected: (box: BoxView, shipBox: ShipBoxView) => void;
    onDisassociateSuccess?: () => void;
}

const emptyGridStyle: CSSProperties = {
    background: "#f8f9fb",
    border: "1px dashed #cfd5dd",
    borderRadius: "10px",
    padding: "1rem",
    color: "#7a869a",
    fontSize: "0.84rem",
};

const TARGET_AMBER = "#f5a300";
const TARGET_AMBER_DARK = "#b66a00";
const TARGET_AMBER_TINT = "#fff9eb";
const HOLD_RED = "#d23232";
const HOLD_RED_DARK = "#a51f1f";

const MINI_SHIPBOX_CELL_SIZE = 22;
const MINI_SHIPBOX_GAP = 4;

const RACK_BOX_BASE_MIN_WIDTH = 128;
const RACK_BOX_BASE_MIN_HEIGHT = 128;

const RACK_BOX_HORIZONTAL_ALLOWANCE = 32;
const RACK_BOX_VERTICAL_ALLOWANCE = 56;

const getMiniShipBoxGridExtent = (slotCount: number) => {
    const count = Math.max(1, slotCount);

    return (
        count * MINI_SHIPBOX_CELL_SIZE +
        Math.max(0, count - 1) * MINI_SHIPBOX_GAP
    );
};

const miniShipBoxGridStyle = (
    shipBoxLayerCount: number,
    shipBoxBoxCount: number
): CSSProperties => {
    const rowCount = Math.max(1, shipBoxLayerCount);
    const columnCount = Math.max(1, shipBoxBoxCount);

    return {
        display: "grid",
        gridTemplateColumns:
            `repeat(${columnCount}, ${MINI_SHIPBOX_CELL_SIZE}px)`,
        gridTemplateRows:
            `repeat(${rowCount}, ${MINI_SHIPBOX_CELL_SIZE}px)`,
        gap: `${MINI_SHIPBOX_GAP}px`,
        width: "max-content",
        marginInline: "auto",
        pointerEvents: "none",
    };
};

const miniShipBoxCellStyle = (
    shipBox: ShipBoxView,
    isTarget: boolean,
    isInSiteHold: boolean,
    hasHeldHolder: boolean
): CSSProperties => {
    const hasItems = Number(shipBox.ShipBoxListCount) > 0;
    const isRelease =
        shipBox.HasReleaseStatus ||
        shipBox.ShipBoxStatus.trim().toUpperCase() === "RELEASE";
    const isAnyHold = isInSiteHold || hasHeldHolder;

    return {
        position: "relative",
        aspectRatio: "1 / 1",
        borderRadius: "2px",
        background: isAnyHold
            ? HOLD_RED
            : isTarget
                ? TARGET_AMBER
            : isRelease
                ? "#16833a"
                : hasItems
                    ? "#0052cc"
                    : "#b7d7ff",
        border: isAnyHold
            ? `1px solid ${HOLD_RED_DARK}`
            : isTarget
                ? `1px solid ${TARGET_AMBER_DARK}`
            : isRelease
                ? "1px solid #0f6b2e"
                : "1px solid #8bbcff",
        boxShadow: isAnyHold
            ? "0 0 0 1px rgba(210,50,50,0.2)"
            : isTarget
                ? "0 0 0 1px rgba(245,163,0,0.25)"
                : undefined,
    };
};

const miniShipBoxEmptyCellStyle: CSSProperties = {
    aspectRatio: "1 / 1",
    borderRadius: "2px",
    background: "#dce9fb",
    border: "1px solid #c5daf5",
};

function MiniShipBoxGrid({
    box,
    shipBoxLayerCount,
    shipBoxBoxCount,
    selectedTargetShipBox,
}: {
    box: BoxView;
    shipBoxLayerCount: number;
    shipBoxBoxCount: number;
    selectedTargetShipBox: ShipBoxView | null;
}) {
    const shipBoxColumnCount = shipBoxBoxCount;
    const shipBoxes = box.ShipBoxes ?? [];
    const rowCount = shipBoxes.reduce(
        (maximum, shipBox) => Math.max(maximum, shipBox.LayerRowNum),
        Math.max(1, shipBoxLayerCount)
    );
    const columnCount = shipBoxes.reduce(
        (maximum, shipBox) => Math.max(maximum, shipBox.LayerColNum),
        Math.max(1, shipBoxBoxCount)
    );
    const totalSlots = rowCount * columnCount;

    const findShipBox = (layerNumber: number, columnNumber: number) => {
        return shipBoxes.find(
            (shipBox) =>
                shipBox.LayerRowNum === layerNumber &&
                shipBox.LayerColNum === columnNumber
        );
    };

    return (
        <div style={miniShipBoxGridStyle(rowCount, columnCount)}>
            {Array.from({ length: totalSlots }, (_, index) => {
                const layerNumber = Math.floor(index / columnCount) + 1;
                const columnNumber = (index % columnCount) + 1;
                const shipBox = findShipBox(layerNumber, columnNumber);
                const isTarget =
                    selectedTargetShipBox?.BoxNo === box.BoxNo &&
                    selectedTargetShipBox?.ShipBoxName === shipBox?.ShipBoxName;
                const isInSiteHold = Boolean(
                    shipBox?.HasInSiteHold ||
                    (shipBox?.InSiteHoldHolders?.length ?? 0) > 0
                );
                const hasHeldHolder = Boolean(shipBox?.HasHeldHolder);

                return (
                    <span
                        key={
                            shipBox?.ShipBoxName ??
                            `empty-shipbox-slot-${box.BoxNo}-${layerNumber}-${columnNumber}`
                        }
                        style={
                            shipBox
                                ? miniShipBoxCellStyle(
                                    shipBox,
                                    isTarget,
                                    isInSiteHold,
                                    hasHeldHolder
                                )
                                : miniShipBoxEmptyCellStyle
                        }
                        title={
                            shipBox
                                ? [
                                    formatShipBoxName(shipBox.LayerRowNum, shipBox.LayerColNum, shipBoxColumnCount),
                                    isInSiteHold
                                        ? `In-site hold: ${(shipBox.InSiteHoldHolders ?? []).join(", ")}`
                                        : hasHeldHolder
                                            ? "On hold"
                                            : "",
                                ].filter(Boolean).join(" · ")
                                : undefined
                        }
                    >
                        {isInSiteHold ? (
                            <i
                                className="fa-solid fa-triangle-exclamation"
                                aria-hidden="true"
                                style={{
                                    position: "absolute",
                                    inset: 0,
                                    display: "grid",
                                    placeItems: "center",
                                    color: "#ffffff",
                                    fontSize: "0.5rem",
                                }}
                            />
                        ) : isTarget ? (
                            <i
                                className="fa-solid fa-check"
                                aria-hidden="true"
                                style={{
                                    position: "absolute",
                                    inset: 0,
                                    display: "grid",
                                    placeItems: "center",
                                    color: "#172b4d",
                                    fontSize: "0.55rem",
                                }}
                            />
                        ) : null}
                    </span>
                );
            })}
        </div>
    );
}

export default function RackPanel({
    recentlyAssignedBoxNo,
    rackNumber,
    layerCount,
    boxCount,
    shipBoxLayerCount,
    shipBoxBoxCount,
    maxItemPerShipBox,
    boxes = [],
    boxSelectionEnabled,
    selectedTargetBox,
    selectedTargetShipBox,
    onTargetShipBoxSelected,
    onDisassociateSuccess,
}: RackPanelProps) {
    const [selectedBox, setSelectedBox] = useState<BoxView | null>(null);
    const columns = Array.from({ length: Math.max(0, boxCount) }, (_, index) => index + 1);
    const layers = Array.from({ length: Math.max(0, layerCount) }, (_, index) => index + 1);

    const visibleShipBoxRowCount = boxes.reduce(
        (maximum, box) =>
            (box.ShipBoxes ?? []).reduce(
                (boxMaximum, shipBox) =>
                    Math.max(
                        boxMaximum,
                        shipBox.LayerRowNum
                    ),
                maximum
            ),
        Math.max(1, shipBoxLayerCount)
    );

    const visibleShipBoxColumnCount = boxes.reduce(
        (maximum, box) =>
            (box.ShipBoxes ?? []).reduce(
                (boxMaximum, shipBox) =>
                    Math.max(
                        boxMaximum,
                        shipBox.LayerColNum
                    ),
                maximum
            ),
        Math.max(1, shipBoxBoxCount)
    );

    const rackBoxMinimumWidth = Math.max(
        RACK_BOX_BASE_MIN_WIDTH,
        getMiniShipBoxGridExtent(
            visibleShipBoxColumnCount
        ) + RACK_BOX_HORIZONTAL_ALLOWANCE
    );

    const rackBoxMinimumHeight = Math.max(
        RACK_BOX_BASE_MIN_HEIGHT,
        getMiniShipBoxGridExtent(
            visibleShipBoxRowCount
        ) + RACK_BOX_VERTICAL_ALLOWANCE
    );

    const findBox = (layerNumber: number, columnNumber: number) => {
        return boxes.find(
            (box) =>
                box.RackNum === rackNumber &&
                box.LayerRowNum === layerNumber &&
                box.LayerColNum === columnNumber
        );
    };

    if (columns.length === 0 || layers.length === 0) {
        return (
            <article style={rackCardStyle}>
                <div style={rackHeaderStyle}>
                    <div>
                        <h3 style={rackTitleStyle}>{formatRackName(rackNumber)}</h3>
                    </div>
                </div>

                <div style={emptyGridStyle}>No rack cells configured.</div>
            </article>
        );
    }

    return (
        <article style={rackCardStyle}>
            <div
                style={{
                    ...rackHeaderStyle,
                    alignItems: "flex-start",
                    paddingBottom: "1rem",
                    borderBottom: "1px solid #dde1e9",
                    marginBottom: "1rem",
                }}
            >
                <div>
                    <h3
                        style={{
                            ...rackTitleStyle,
                            color: "#0b1f55",
                            fontSize: "0.98rem",
                        }}
                    >
                        {formatRackName(rackNumber)}
                    </h3>

                    {selectedTargetBox?.RackNum === rackNumber &&
                        selectedTargetShipBox && (
                            <p className="rack-target-locator">
                                <span>Next placement</span>
                                <span aria-hidden="true">&middot;</span>
                                <strong>{formatBoxName(selectedTargetBox.LayerRowNum, selectedTargetBox.LayerColNum, boxCount)}</strong>
                                <i
                                    className="fa-solid fa-arrow-right"
                                    aria-hidden="true"
                                />
                                <strong>{formatShipBoxName(selectedTargetShipBox.LayerRowNum, selectedTargetShipBox.LayerColNum, shipBoxBoxCount)}</strong>
                            </p>
                        )}
                </div>
            </div>

            <div style={rackScrollStyle}>
                <div
                    style={{
                        ...rackOverviewGridStyle(
                            columns.length,
                            layers.length,
                            rackBoxMinimumWidth,
                            rackBoxMinimumHeight
                        ),
                        gridTemplateRows:
                            `repeat(${Math.max(0, layers.length)}, minmax(${rackBoxMinimumHeight}px, auto))`,
                    }}
                >
                    {layers.map((layerNumber) => (
                        <Fragment key={`rack-${rackNumber}-layer-${layerNumber}`}>
                            <div
                                style={{
                                    ...rowLabelCellStyle,
                                    flexDirection: "column",
                                    justifyContent: "center",
                                    paddingLeft: 0,
                                    border: 0,
                                    background: "transparent",
                                    boxShadow: "none",
                                    color: "#0b1f55",
                                    lineHeight: 1.15,
                                    textTransform: "uppercase",
                                }}
                            >
                                <span>Layer</span>
                                <strong>{layerNumber}</strong>
                            </div>

                            {columns.map((columnNumber) => {
                                const box = findBox(layerNumber, columnNumber);
                                const isSelectedTarget = selectedTargetBox?.BoxNo === box?.BoxNo;
                                const isRecentlyAssigned = recentlyAssignedBoxNo === box?.BoxNo;
                                const hasShipBoxes = Boolean(
                                    box &&
                                    ((box.ShipBoxes?.length ?? 0) > 0 ||
                                        Number(box.BoxListCount) > 0)
                                );
                                const isShipBoxModalOpen =
                                    selectedBox?.BoxNo === box?.BoxNo;
                                const inSiteHoldHolders = box
                                    ? Array.from(
                                        new Set(
                                            (box.ShipBoxes ?? []).flatMap(
                                                (shipBox) =>
                                                    shipBox.InSiteHoldHolders ?? []
                                            )
                                        )
                                    )
                                    : [];
                                const inSiteHoldCount =
                                    inSiteHoldHolders.length;
                                const hasAnyHeldHolder = box
                                    ? (box.ShipBoxes ?? []).some(
                                        (shipBox) => shipBox.HasHeldHolder
                                    )
                                    : false;

                                return (
                                    <button
                                        className={[
                                            "rack-box-cell",
                                            hasShipBoxes ? "rack-box-cell--interactive" : "",
                                            isSelectedTarget ? "rack-box-cell--target" : "",
                                            isRecentlyAssigned
                                                ? "rack-box-cell--recently-assigned"
                                                : "",
                                        ]
                                            .filter(Boolean)
                                            .join(" ")}
                                        type="button"
                                        key={`rack-${rackNumber}-layer-${layerNumber}-box-${columnNumber}`}
                                        disabled={!hasShipBoxes}
                                        onClick={() => {
                                            if (!box || !hasShipBoxes) return;
                                            setSelectedBox(box);
                                        }}
                                        style={{
                                            ...(box
                                                ? {
                                                    ...getEmptyCellStyle(),
                                                    background: isSelectedTarget
                                                        ? TARGET_AMBER_TINT
                                                        : "#ffffff",
                                                    border: isRecentlyAssigned
                                                        ? "3px solid #16833a"
                                                        : isSelectedTarget
                                                            ? `2px solid ${TARGET_AMBER}`
                                                            : "1px solid #d9e2ef",
                                                    padding: "0.5rem",
                                                    overflow:
                                                        isSelectedTarget || isRecentlyAssigned
                                                            ? "visible"
                                                            : "hidden",
                                                    boxShadow: isRecentlyAssigned
                                                        ? "0 4px 12px rgba(22,131,58,0.35)"
                                                        : isSelectedTarget
                                                            ? "0 5px 14px rgba(245,163,0,0.25)"
                                                            : "0 1px 3px rgba(23,43,77,0.08)",
                                                }
                                                : getEmptyCellStyle()),
                                            minHeight: `${rackBoxMinimumHeight}px`,
                                            position: "relative",
                                            cursor: hasShipBoxes ? "pointer" : "default",
                                        }}
                                        aria-haspopup={hasShipBoxes ? "dialog" : undefined}
                                        aria-expanded={
                                            hasShipBoxes ? isShipBoxModalOpen : undefined
                                        }
                                        aria-current={
                                            isSelectedTarget ? "true" : undefined
                                        }
                                        aria-label={
                                            (!box
                                                ? "Empty rack cell"
                                                : hasShipBoxes
                                                    ? `Open ShipBoxes for ${formatBoxName(box.LayerRowNum, box.LayerColNum, boxCount)}`
                                                    : `${formatBoxName(box.LayerRowNum, box.LayerColNum, boxCount)} has no ShipBoxes`) +
                                            (inSiteHoldCount > 0
                                                ? `, ${inSiteHoldCount} ${inSiteHoldCount === 1 ? "holder" : "holders"} on in-site hold`
                                                : hasAnyHeldHolder
                                                    ? ", has holders on hold"
                                                    : "")
                                        }
                                        title={
                                            box && !hasShipBoxes
                                                ? "This Box does not contain any ShipBoxes."
                                                : undefined
                                        }
                                    >
                                        {box && (
                                            <>
                                                <span className="rack-box-content rack-box-content--expanded-mini-grid">
                                                    <span className="rack-box-name">
                                                        {formatBoxName(box.LayerRowNum, box.LayerColNum, boxCount)}
                                                    </span>

                                                    <MiniShipBoxGrid
                                                        box={box}
                                                        shipBoxLayerCount={shipBoxLayerCount}
                                                        shipBoxBoxCount={shipBoxBoxCount}
                                                        selectedTargetShipBox={
                                                            selectedTargetShipBox
                                                        }
                                                    />
                                                </span>

                                                {inSiteHoldCount > 0 && (
                                                    <span
                                                        className="rack-box-in-site-hold-badge"
                                                        title={`In-site hold: ${inSiteHoldHolders.join(", ")}`}
                                                    >
                                                        IN-SITE
                                                    </span>
                                                )}

                                                {isSelectedTarget && (
                                                    <span
                                                        className="rack-next-box-ribbon"
                                                        aria-hidden="true"
                                                    >
                                                        NEXT BOX
                                                    </span>
                                                )}

                                                {isRecentlyAssigned && (
                                                    <span
                                                        style={{
                                                            position: "absolute",
                                                            left: "0.35rem",
                                                            bottom: "0.3rem",
                                                            background: "#d8f3df",
                                                            border: "1px solid #16833a",
                                                            borderRadius: "6px",
                                                            color: "#0f6b2e",
                                                            fontSize: "0.62rem",
                                                            fontWeight: 800,
                                                            padding: "0.08rem 0.35rem",
                                                            zIndex: 7,
                                                            pointerEvents: "none",
                                                        }}
                                                    >
                                                        Assigned
                                                    </span>
                                                )}

                                                {hasShipBoxes && (
                                                    <span
                                                        className="rack-box-hover-action"
                                                        aria-hidden="true"
                                                    >
                                                        <i className="fa-solid fa-chevron-right" />
                                                    </span>
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

            {selectedBox && (
                <ShipBoxGridModal
                    box={selectedBox}
                    layerCount={shipBoxLayerCount}
                    boxCount={shipBoxBoxCount}
                    rackBoxColumnCount={boxCount}
                    maxItemPerShipBox={maxItemPerShipBox}
                    shipBoxSelectionEnabled={boxSelectionEnabled}
                    selectedTargetShipBox={selectedTargetShipBox}
                    onTargetShipBoxSelected={onTargetShipBoxSelected}
                    onClose={() => setSelectedBox(null)}
                    onDisassociateSuccess={onDisassociateSuccess}
                />
            )}
        </article>
    );
}
