import { Fragment, type CSSProperties } from "react";
import type { BoxView } from "../../types/stacker";
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
} from "./rackGridStyles";

interface RackPanelProps {
    rackNumber: number;
    layerCount: number;
    boxCount: number;
    boxes?: BoxView[];
}

const emptyGridStyle: CSSProperties = {
    background: "#f8f9fb",
    border: "1px dashed #cfd5dd",
    borderRadius: "10px",
    padding: "1rem",
    color: "#7a869a",
    fontSize: "0.84rem",
};

export default function RackPanel({ rackNumber, layerCount, boxCount, boxes = [], }: RackPanelProps)
{
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

                                return (
                                    <div
                                        key={`rack-${rackNumber}-layer-${layerNumber}-box-${columnNumber}`}
                                        style={
                                            box
                                                ? getMappedCellStyle(box)
                                                : getEmptyCellStyle()
                                        }
                                        aria-label={`Rack ${rackNumber}, Layer ${layerNumber}, Box ${columnNumber}`}
                                        title={`Rack ${rackNumber} / Layer ${layerNumber} / Box ${columnNumber}`}
                                    >
                                        {box && (
                                            <span
                                                style={{
                                                    color: "#ffffff",
                                                    fontSize: "0.68rem",
                                                    fontWeight: 700,
                                                    lineHeight: 1.15,
                                                    textAlign: "center",
                                                    textShadow: "0 1px 2px rgba(0,0,0,0.35)",
                                                    overflowWrap: "anywhere",
                                                }}
                                            >
                                                {box.BoxNo} ({Number(box.BoxListPercentage).toFixed(2)}%)
                                            </span>
                                        )}
                                    </div>
                                );
                            })}
                        </Fragment>
                    ))}
                </div>
            </div>
        </article>
    );
}