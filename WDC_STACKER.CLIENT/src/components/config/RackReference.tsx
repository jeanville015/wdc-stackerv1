import type { CSSProperties } from "react";

const LAYER_CAP = 3;
const BOX_CAP = 4;
const ITEM_CAP = 10;
const RACK_BLUE = "#0b66d8";
const LAYER_TEAL = "#33b7b4";
const BOX_ORANGE = "#f0b84b";
const ITEM_PURPLE = "#8d3fd1";
const BLUE_DARK = "#0052cc";
const BLUE_LIGHT = "#cfe3ff";

interface RackReferenceProps {
    layerCount: number;
    boxCount: number;
    maxItems: number;
}

const clampVisible = (value: number, cap: number) =>
    Math.min(Math.max(1, Number(value) || 1), cap);

const cellShell: CSSProperties = {
    position: "relative",
    minHeight: "54px",
    border: `2px solid ${BOX_ORANGE}`,
    borderRadius: "7px",
    background: "#eaf3ff",
    overflow: "hidden",
};

function HolderSegments({ ratio, maxItems, label }: { ratio: number; maxItems: number; label: string }) {
    const visibleCapacity = Math.min(Math.max(1, Number(maxItems) || 1), ITEM_CAP);
    const occupied = Math.min(visibleCapacity, Math.max(0, Math.round(visibleCapacity * ratio)));

    if (occupied === 0) {
        return (
            <span
                title={`${label}: empty holder box`}
                style={{
                    position: "absolute",
                    inset: 0,
                    display: "flex",
                    alignItems: "center",
                    justifyContent: "center",
                    color: "#5e6c84",
                    fontSize: "0.66rem",
                    fontWeight: 800,
                }}
            >
                Empty
            </span>
        );
    }

    return (
        <>
            <span
                aria-hidden="true"
                style={{
                    position: "absolute",
                    inset: "4px",
                    display: "grid",
                    gridTemplateColumns: `repeat(${visibleCapacity}, minmax(0, 1fr))`,
                    gap: "2px",
                    padding: "3px",
                    border: `2px solid ${ITEM_PURPLE}`,
                    borderRadius: "6px",
                    background: BLUE_LIGHT,
                }}
            >
                {Array.from({ length: visibleCapacity }, (_, index) => (
                    <span
                        key={index}
                        style={{
                            minWidth: 0,
                            borderRadius: "3px",
                            background: index < occupied ? BLUE_DARK : BLUE_LIGHT,
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
                    color: "#ffffff",
                    fontSize: "0.58rem",
                    fontWeight: 800,
                    lineHeight: 1.05,
                    textAlign: "center",
                    textShadow: "0 1px 2px rgba(0,0,0,0.6)",
                    overflowWrap: "anywhere",
                }}
            >
                <span>{label}</span>
                <small>{occupied}/{visibleCapacity}</small>
            </span>
        </>
    );
}

export default function RackReference({ layerCount, boxCount, maxItems }: RackReferenceProps) {
    const visibleLayers = clampVisible(layerCount, LAYER_CAP);
    const visibleBoxes = clampVisible(boxCount, BOX_CAP);
    const ratios = [0.7, 0.4, 1, 0];

    return (
        <div
            aria-label="Rack No. 1 reference"
            style={{
                border: `3px solid ${RACK_BLUE}`,
                borderRadius: "8px",
                padding: "16px",
                background: "#ffffff",
                minWidth: 0,
            }}
        >
            <div style={{ marginBottom: "12px", color: "#172b4d", fontWeight: 800, letterSpacing: "0.08em" }}>
                RACK NO. 1
            </div>

            <div
                style={{
                    display: "grid",
                    gridTemplateColumns: `76px repeat(${visibleBoxes}, minmax(0, 1fr))`,
                    gap: "6px",
                    minWidth: 0,
                }}
            >
                <span />
                {Array.from({ length: visibleBoxes }, (_, column) => (
                    <span
                        key={column}
                        style={{
                            display: "flex",
                            alignItems: "center",
                            justifyContent: "center",
                            minHeight: "32px",
                            border: `2px solid ${BOX_ORANGE}`,
                            borderRadius: "7px",
                            color: "#172b4d",
                            fontSize: "0.7rem",
                            fontWeight: 800,
                        }}
                    >
                        {column + 1}
                    </span>
                ))}

                {Array.from({ length: visibleLayers }, (_, layer) => (
                    <div
                        key={layer}
                        style={{
                            gridColumn: `1 / -1`,
                            display: "grid",
                            gridTemplateColumns: `76px repeat(${visibleBoxes}, minmax(0, 1fr))`,
                            gap: "6px",
                            padding: "6px",
                            border: `3px solid ${LAYER_TEAL}`,
                            borderRadius: "8px",
                        }}
                    >
                        <span
                            style={{
                                display: "flex",
                                alignItems: "center",
                                paddingLeft: "8px",
                                color: "#172b4d",
                                fontSize: "0.68rem",
                                fontWeight: 800,
                            }}
                        >
                            Layer {layer + 1}
                        </span>

                        {Array.from({ length: visibleBoxes }, (_, column) => {
                            const label = `R01L${String(layer + 1).padStart(2, "0")}C${String(column + 1).padStart(2, "0")}`;
                            const ratio = ratios[column] ?? 0;

                            return (
                                <div key={label} style={ratio === 0 ? { ...cellShell, background: "#e8e8e8", borderStyle: "dashed" } : cellShell}>
                                    <HolderSegments ratio={ratio} maxItems={maxItems} label={label} />
                                </div>
                            );
                        })}
                    </div>
                ))}
            </div>
        </div>
    );
}
