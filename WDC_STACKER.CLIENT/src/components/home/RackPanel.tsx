import { Fragment, useState, type CSSProperties } from "react";
import BoxAssignmentsModal from "./BoxAssignmentsModal";
import SegmentedBox from "./SegmentedBox";
import type { BoxView } from "../../types/stacker";
import { formatRackName } from "../../utils/nameTransformers";
import {
    columnLabelCellStyle,
    cornerCellStyle,
    getMappedCellStyle,
    getEmptyCellStyle,
    rackCardStyle,
    rackGridStyle,
    rackHeaderStyle,
    rackScrollStyle,
    rackTitleStyle,
    rowLabelCellStyle,
    TARGET_AMBER,
    TARGET_AMBER_DARK,
} from "./rackGridStyles";

interface RackPanelProps {
    recentlyAssignedBoxNo?: string | null;
    rackNumber: number;
    layerCount: number;
    boxCount: number;
    maxItemPerBox: number;
    boxes?: BoxView[];
    selectedTargetBox?: BoxView | null;
    isExistingHolderLocation?: boolean;
    onBoxesChanged: (boxes: BoxView[]) => void;
}

const emptyGridStyle: CSSProperties = {
    background: "#f8f9fb",
    border: "1px dashed #cfd5dd",
    borderRadius: "10px",
    padding: "1rem",
    color: "#7a869a",
    fontSize: "0.84rem",
};

export default function RackPanel({ recentlyAssignedBoxNo, rackNumber, layerCount, boxCount, maxItemPerBox, boxes = [], selectedTargetBox, isExistingHolderLocation, onBoxesChanged, }: RackPanelProps)
{
    const [selectedBoxName, setSelectedBoxName] = useState<string | null>(null);
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
                        <h3 style={rackTitleStyle}>{formatRackName(rackNumber)}</h3>
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
                    <h3 style={rackTitleStyle}>{formatRackName(rackNumber)}</h3>
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
                                const isRecentlyAssigned = recentlyAssignedBoxNo === box?.BoxNo;
                                const isTarget = selectedTargetBox?.BoxNo === box?.BoxNo;
                                const showCheckIcon = isTarget && !isRecentlyAssigned && !isExistingHolderLocation;

                                return (
                                    <button
                                        type="button"
                                        key={`rack-${rackNumber}-layer-${layerNumber}-box-${columnNumber}`}
                                        disabled={!box}
                                        onClick={() => box && setSelectedBoxName(box.BoxNo)}
                                        style={{
                                            ...(box ? getMappedCellStyle(box, isRecentlyAssigned, isTarget) : getEmptyCellStyle()),
                                            position: "relative",
                                            cursor: box ? "pointer" : "default",
                                        }}
                                        aria-label={
                                            box
                                                ? `Open assignments for ${box.BoxNo}`
                                                : `Empty rack cell`
                                        }
                                    >
                                        {box && (
                                            <>
                                                <SegmentedBox box={box} maxItemPerBox={maxItemPerBox} />

                                                {showCheckIcon && (
                                                    <span
                                                        style={{
                                                            position: "absolute",
                                                            top: "-9px",
                                                            right: "-9px",
                                                            width: "22px",
                                                            height: "22px",
                                                            borderRadius: "50%",
                                                            background: TARGET_AMBER,
                                                            border: `2px solid ${TARGET_AMBER_DARK}`,
                                                            color: "#172b4d",
                                                            display: "flex",
                                                            alignItems: "center",
                                                            justifyContent: "center",
                                                            fontSize: "0.68rem",
                                                            fontWeight: 800,
                                                            zIndex: 7,
                                                            pointerEvents: "none",
                                                            boxShadow: "0 2px 6px rgba(245,163,0,0.4)",
                                                        }}
                                                    >
                                                        <i className="fa-solid fa-check" aria-hidden="true" />
                                                    </span>
                                                )}

                                                {isRecentlyAssigned && (
                                                    <>
                                                        <span
                                                            style={{
                                                                position: "absolute",
                                                                top: "-10px",
                                                                right: "-10px",
                                                                width: "26px",
                                                                height: "26px",
                                                                borderRadius: "50%",
                                                                background: "#16833a",
                                                                border: "3px solid #ffffff",
                                                                color: "#ffffff",
                                                                display: "flex",
                                                                alignItems: "center",
                                                                justifyContent: "center",
                                                                fontSize: "0.82rem",
                                                                fontWeight: 800,
                                                                zIndex: 7,
                                                                pointerEvents: "none",
                                                            }}
                                                        >
                                                            ✓
                                                        </span>

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
            {selectedBoxName && (
                <BoxAssignmentsModal
                    boxName={selectedBoxName}
                    onClose={() => setSelectedBoxName(null)}
                    onBoxesChanged={onBoxesChanged}
                />
            )}
        </article>
    );
}
