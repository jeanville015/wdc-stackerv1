import type { CSSProperties } from "react";

const LAYER_CAP = 3;
const BOX_CAP = 4;
const ITEM_CAP = 10;
const BLUE_DARK = "#0052cc";
const BLUE_LIGHT = "#cfe3ff";
const GREEN_DARK = "#16833a";
const GREEN_LIGHT = "#d8f3df";
const REFERENCE_BLUE = "#0b66d8";
const LAYER_ACCENT = "#16a6a2";
const BOX_ACCENT = "#e49a12";
const ITEM_ACCENT = "#0b66d8";
const EMPTY_BORDER = "#cfd5dd";

interface ShipBoxReferenceProps {
    layerCount: number;
    boxCount: number;
    maxItems: number;
}

const clampVisible = (value: number, cap: number) =>
    Math.min(Math.max(1, Number(value) || 1), cap);

const cellShell: CSSProperties = {
    position: "relative",
    minHeight: "54px",
    border: `2px solid ${BOX_ACCENT}`,
    borderRadius: "7px",
    background: "#e8e8e8",
    overflow: "hidden",
};

function ShipBoxCell({ ratio, maxItems, label, release }: { ratio: number; maxItems: number; label: string; release: boolean }) {
    const visibleCapacity = Math.min(Math.max(1, Number(maxItems) || 1), ITEM_CAP);
    const occupied = Math.min(visibleCapacity, Math.max(0, Math.round(visibleCapacity * ratio)));
    const filledColor = release ? GREEN_DARK : BLUE_DARK;
    const availableColor = release ? GREEN_LIGHT : BLUE_LIGHT;

    if (occupied === 0) {
        return (
            <div
                style={{ ...cellShell, border: `2px dashed ${EMPTY_BORDER}` }}
                title={`${label}: empty ShipBox`}
            >
                <span
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
            </div>
        );
    }

    return (
        <div style={{ ...cellShell, background: availableColor }}>
            <span
                aria-hidden="true"
                style={{
                    position: "absolute",
                    inset: "4px",
                    display: "grid",
                    gridTemplateColumns: `repeat(${visibleCapacity}, minmax(0, 1fr))`,
                    gap: "2px",
                    padding: "3px",
                    borderRadius: "6px",
                    background: availableColor,
                    border: `2px solid ${ITEM_ACCENT}`,
                }}
            >
                {Array.from({ length: visibleCapacity }, (_, index) => (
                    <span
                        key={index}
                        style={{
                            minWidth: 0,
                            borderRadius: "3px",
                            background: index < occupied ? filledColor : availableColor,
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
        </div>
    );
}

export default function ShipBoxReference({ layerCount, boxCount, maxItems }: ShipBoxReferenceProps) {
    const visibleLayers = clampVisible(layerCount, LAYER_CAP);
    const visibleBoxes = clampVisible(boxCount, BOX_CAP);
    const occupancy: Record<string, number> = {
        "1-1": 0.7,
        "1-2": 0.4,
        "2-2": 0.2,
        "3-3": 0.5,
    };

    return (
        <div
            aria-label="ShipBoxes R01L01C01 reference"
            style={{
                border: `2px solid ${REFERENCE_BLUE}`,
                borderRadius: "8px",
                background: "#ffffff",
                overflow: "hidden",
                minWidth: 0,
            }}
        >
            <div
                style={{
                    display: "flex",
                    alignItems: "center",
                    justifyContent: "space-between",
                    minHeight: "54px",
                    padding: "0 14px",
                    borderBottom: `2px solid ${REFERENCE_BLUE}`,
                    color: "#172b4d",
                    fontWeight: 800,
                }}
            >
                <span>ShipBoxes: R01L01C01</span>
                <button type="button" aria-label="Close ShipBoxes reference" className="btn-close" />
            </div>

            <div style={{ padding: "14px" }}>
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
                                border: `2px solid ${BOX_ACCENT}`,
                                borderRadius: "7px",
                                background: "#ffffff",
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
                                gridColumn: "1 / -1",
                                display: "grid",
                                gridTemplateColumns: `76px repeat(${visibleBoxes}, minmax(0, 1fr))`,
                                gap: "6px",
                                padding: "6px",
                                border: `3px solid ${LAYER_ACCENT}`,
                                borderRadius: "8px",
                            }}
                        >
                            <span
                                style={{
                                    display: "flex",
                                    alignItems: "center",
                                    paddingLeft: "8px",
                                    border: `2px solid ${LAYER_ACCENT}`,
                                    borderRadius: "7px",
                                    background: "#e8e8e8",
                                    color: "#5e6c84",
                                    fontSize: "0.68rem",
                                    fontWeight: 800,
                                }}
                            >
                                Layer {layer + 1}
                            </span>

                            {Array.from({ length: visibleBoxes }, (_, column) => {
                                const label = `S01L${String(layer + 1).padStart(2, "0")}C${String(column + 1).padStart(2, "0")}`;
                                const ratio = occupancy[`${layer + 1}-${column + 1}`] ?? 0;

                                return (
                                    <ShipBoxCell
                                        key={label}
                                        ratio={ratio}
                                        maxItems={maxItems}
                                        label={label}
                                        release={layer === 2 && column === 2}
                                    />
                                );
                            })}
                        </div>
                    ))}
                </div>
            </div>
        </div>
    );
}
