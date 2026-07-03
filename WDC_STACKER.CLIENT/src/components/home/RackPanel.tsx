import { Fragment, useState, type CSSProperties } from "react";
import BoxAssignmentsModal from "./BoxAssignmentsModal";
import type { BoxView } from "../../types/stacker";
import {
    columnLabelCellStyle,
    cornerCellStyle,
    getBoxHighlightColor,
    getCornerHighlightStyle,
    getMappedCellStyle,
    getEmptyCellStyle,
    rackCardStyle,
    rackGridStyle,
    rackHeaderStyle,
    rackScrollStyle,
    rackTitleStyle,
    rowLabelCellStyle, 
} from "./rackGridStyles";

interface RackPanelProps {
    rackNumber: number;
    layerCount: number;
    boxCount: number;
    maxItemPerBox: number;
    boxes?: BoxView[];
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

export default function RackPanel({ rackNumber, layerCount, boxCount, maxItemPerBox, boxes = [], onBoxesChanged, }: RackPanelProps)
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
                                const box = findBox(layerNumber, columnNumber); const percentage = box
                                    ? Math.min(Math.max(Number(box.BoxListPercentage), 0), 100)
                                    : 0;
                                const label = box
                                    ? `${box.BoxNo} (${box.BoxListCount}/${maxItemPerBox})`
                                    : "";

                                return (
                                    <button
                                        type="button"
                                        key={`rack-${rackNumber}-layer-${layerNumber}-box-${columnNumber}`}
                                        disabled={!box}
                                        onClick={() => box && setSelectedBoxName(box.BoxNo)}
                                        style={{
                                            ...(box ? getMappedCellStyle(box) : getEmptyCellStyle()),
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
                                                <span
                                                    style={{
                                                        position: "absolute",
                                                        inset: 0,
                                                        display: "flex",
                                                        alignItems: "center",
                                                        justifyContent: "center",
                                                        padding: "0.25rem",
                                                        color: "#003d99",
                                                        fontSize: "0.68rem",
                                                        fontWeight: 700,
                                                        lineHeight: 1.15,
                                                        textAlign: "center",
                                                        overflowWrap: "anywhere",
                                                        pointerEvents: "none",
                                                    }}
                                                >
                                                    {label}
                                                </span>

                                                <span
                                                    style={{
                                                        position: "absolute",
                                                        inset: 0,
                                                        display: "flex",
                                                        alignItems: "center",
                                                        justifyContent: "center",
                                                        padding: "0.25rem",
                                                        color: "#ffffff",
                                                        fontSize: "0.68rem",
                                                        fontWeight: 700,
                                                        lineHeight: 1.15,
                                                        textAlign: "center",
                                                        overflowWrap: "anywhere",
                                                        pointerEvents: "none",
                                                        clipPath: `inset(0 ${100 - percentage}% 0 0)`,
                                                        textShadow: "0 1px 2px rgba(0,0,0,0.35)",
                                                    }}
                                                >
                                                    {label}
                                                </span>

                                                {box.IsSuggestedTarget && (
                                                    <>
                                                        <span style={getCornerHighlightStyle("topLeft", getBoxHighlightColor(box))} />
                                                        <span style={getCornerHighlightStyle("topRight", getBoxHighlightColor(box))} />
                                                        <span style={getCornerHighlightStyle("bottomLeft", getBoxHighlightColor(box))} />
                                                        <span style={getCornerHighlightStyle("bottomRight", getBoxHighlightColor(box))} />
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