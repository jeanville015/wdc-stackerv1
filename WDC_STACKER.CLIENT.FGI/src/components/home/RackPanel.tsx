import { Fragment, useState, type CSSProperties } from "react";
import ShipBoxGridModal from "./ShipBoxGridModal";
import type { BoxView, ShipBoxView } from "../../types/stacker";
import {
    columnLabelCellStyle,
    cornerCellStyle,
    getBoxHighlightColor,
    getCornerHighlightStyle,
    getEmptyCellStyle,
    rackCardStyle,
    rackGridStyle,
    rackHeaderStyle,
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
}

const emptyGridStyle: CSSProperties = {
    background: "#f8f9fb",
    border: "1px dashed #cfd5dd",
    borderRadius: "10px",
    padding: "1rem",
    color: "#7a869a",
    fontSize: "0.84rem",
};

const MINI_SHIPBOX_LAYER_COUNT = 3;

const miniShipBoxGridStyle = (shipBoxBoxCount: number): CSSProperties => ({
    display: "grid",
    gridTemplateColumns: `repeat(${Math.max(1, shipBoxBoxCount)}, minmax(0, 1fr))`,
    gridTemplateRows: `repeat(${MINI_SHIPBOX_LAYER_COUNT}, minmax(0, 1fr))`,
    gap: "3px",
    width: "82%",
    maxWidth: "58px",
    pointerEvents: "none",
});

const miniShipBoxCellStyle = (isSuggested?: boolean): CSSProperties => ({
    aspectRatio: "1 / 1",
    borderRadius: "2px",
    background: isSuggested ? "#003d99" : "#4c9aff",
    border: isSuggested ? "1px solid #001f5c" : "1px solid #8bbcff",
});

const miniShipBoxEmptyCellStyle: CSSProperties = {
    aspectRatio: "1 / 1",
    borderRadius: "2px",
    background: "#d8dde6",
    border: "1px solid #c1c7d0",
};

function MiniShipBoxGrid({
    box,
    shipBoxBoxCount,
}: {
    box: BoxView;
    shipBoxBoxCount: number;
}) {
    const shipBoxes = box.ShipBoxes ?? [];
    const columnCount = Math.max(1, shipBoxBoxCount);
    const totalSlots = MINI_SHIPBOX_LAYER_COUNT * columnCount;
    const shouldCollapse = totalSlots > 15;
    const visibleSlotCount = shouldCollapse ? 13 : totalSlots;

    const findShipBox = (layerNumber: number, columnNumber: number) => {
        return shipBoxes.find(
            (shipBox) =>
                shipBox.LayerRowNum === layerNumber &&
                shipBox.LayerColNum === columnNumber
        );
    };

    return (
        <div style={miniShipBoxGridStyle(columnCount)}>
            {Array.from({ length: visibleSlotCount }, (_, index) => {
                const layerNumber = Math.floor(index / columnCount) + 1;
                const columnNumber = (index % columnCount) + 1;
                const shipBox = findShipBox(layerNumber, columnNumber);

                return (
                    <span
                        key={
                            shipBox?.ShipBoxName ??
                            `empty-shipbox-slot-${box.BoxNo}-${layerNumber}-${columnNumber}`
                        }
                        style={
                            shipBox
                                ? miniShipBoxCellStyle(shipBox.IsSuggestedTarget)
                                : miniShipBoxEmptyCellStyle
                        }
                    />
                );
            })}

            {shouldCollapse && (
                <span
                    style={{
                        color: "#003d99",
                        fontSize: "0.72rem",
                        fontWeight: 800,
                        lineHeight: 1,
                        gridColumn: "span 2",
                    }}
                >
                    ...
                </span>
            )}
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
}: RackPanelProps) {
    const [selectedBox, setSelectedBox] = useState<BoxView | null>(null);
    const columns = Array.from({ length: Math.max(0, boxCount) }, (_, index) => index + 1);
    const layers = Array.from({ length: Math.max(0, layerCount) }, (_, index) => index + 1);

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
                        <h3 style={rackTitleStyle}>Rack No. {rackNumber}</h3>
                    </div>
                </div>

                <div style={emptyGridStyle}>No rack cells configured.</div>
            </article>
        );
    }

    return (
        <article style={rackCardStyle}>
            <div style={rackHeaderStyle}>
                <div>
                    <h3 style={rackTitleStyle}>Rack No. {rackNumber}</h3>
                </div>
            </div>

            <div style={rackScrollStyle}>
                <div style={rackGridStyle(columns.length, layers.length)}>
                    <div style={cornerCellStyle} />

                    {columns.map((columnNumber) => (
                        <div
                            key={`rack-${rackNumber}-column-${columnNumber}`}
                            style={columnLabelCellStyle}
                        >
                            {columnNumber}
                        </div>
                    ))}

                    {layers.map((layerNumber) => (
                        <Fragment key={`rack-${rackNumber}-layer-${layerNumber}`}>
                            <div style={rowLabelCellStyle}>Layer {layerNumber}</div>

                            {columns.map((columnNumber) => {
                                const box = findBox(layerNumber, columnNumber);
                                const isSelectedTarget = selectedTargetBox?.BoxNo === box?.BoxNo;
                                const isRecentlyAssigned = recentlyAssignedBoxNo === box?.BoxNo;

                                return (
                                    <button
                                        type="button"
                                        key={`rack-${rackNumber}-layer-${layerNumber}-box-${columnNumber}`}
                                        disabled={!box}
                                        onClick={() => {
                                            if (!box) return;
                                            setSelectedBox(box);
                                        }}
                                        style={{
                                            ...(box
                                                ? {
                                                    ...getEmptyCellStyle(),
                                                    background: "#eaf3ff",
                                                    border: isRecentlyAssigned ? "3px solid #16833a" : "1px solid #8bbcff",
                                                    padding: "0.25rem",
                                                    outline: "none",
                                                    outlineOffset: "0",
                                                    overflow: isSelectedTarget || isRecentlyAssigned ? "visible" : "hidden",
                                                    boxShadow: isRecentlyAssigned
                                                        ? "inset 0 1px 0 rgba(255,255,255,0.85), 0 4px 12px rgba(22,131,58,0.35)"
                                                        : isSelectedTarget
                                                            ? "inset 0 1px 0 rgba(255,255,255,0.85), 0 4px 10px rgba(0,82,204,0.35)"
                                                            : "inset 0 1px 0 rgba(255,255,255,0.85), 0 1px 2px rgba(23,43,77,0.06)",
                                                }
                                                : getEmptyCellStyle()),
                                            position: "relative",
                                            cursor: box ? "pointer" : "default",
                                        }}
                                        aria-label={
                                            box
                                                ? `Open ShipBoxes for ${box.BoxNo}`
                                                : "Empty rack cell"
                                        }
                                    >
                                        {box && (
                                            <>
                                                <span
                                                    style={{
                                                        position: "absolute",
                                                        inset: 0,
                                                        display: "flex",
                                                        alignItems: "center",
                                                        justifyContent: "center",
                                                        padding: "0.25rem",
                                                        pointerEvents: "none",
                                                    }}
                                                >
                                                    <MiniShipBoxGrid
                                                        box={box}
                                                        shipBoxBoxCount={shipBoxBoxCount}
                                                    />
                                                </span>

                                                {isSelectedTarget && (
                                                    <>
                                                        <span style={getCornerHighlightStyle("topLeft", getBoxHighlightColor(box))} />
                                                        <span style={getCornerHighlightStyle("topRight", getBoxHighlightColor(box))} />
                                                        <span style={getCornerHighlightStyle("bottomLeft", getBoxHighlightColor(box))} />
                                                        <span style={getCornerHighlightStyle("bottomRight", getBoxHighlightColor(box))} />
                                                    </>
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
                    maxItemPerShipBox={maxItemPerShipBox}
                    shipBoxSelectionEnabled={boxSelectionEnabled}
                    selectedTargetShipBox={selectedTargetShipBox}
                    onTargetShipBoxSelected={onTargetShipBoxSelected}
                    onClose={() => setSelectedBox(null)}
                />
            )}
        </article>
    );
}
