import type { CSSProperties } from "react";

const LAYER_CAP = 3;
const BOX_CAP = 4;
const ITEM_CAP = 10;
const RACK_BLUE = "#0b66d8";
const LAYER_TEAL = "#33b7b4";
const BOX_ORANGE = "#f0b84b";
const ITEM_PURPLE = "#8d3fd1";
const MINI_BLUE = "#4c9aff";
const MINI_BLUE_BORDER = "#8bbcff";

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
    display: "flex",
    alignItems: "center",
    justifyContent: "center",
};

const miniItemGridStyle = (columnCount: number, rowCount: number): CSSProperties => ({
    display: "grid",
    gridTemplateColumns: `repeat(${columnCount}, minmax(0, 1fr))`,
    gridTemplateRows: `repeat(${rowCount}, minmax(0, 1fr))`,
    gap: "3px",
    width: "82%",
    maxWidth: "58px",
    padding: "3px",
    border: `2px solid ${ITEM_PURPLE}`,
    borderRadius: "6px",
    pointerEvents: "none",
});

function MiniItemGrid({ maxItems }: { maxItems: number }) {
    const visibleItems = Math.min(Math.max(1, Number(maxItems) || 1), ITEM_CAP);
    const columnCount = Math.min(5, visibleItems);
    const rowCount = Math.ceil(visibleItems / columnCount);

    return (
        <div style={miniItemGridStyle(columnCount, rowCount)}>
            {Array.from({ length: visibleItems }, (_, index) => (
                <span
                    key={index}
                    style={{
                        aspectRatio: "1 / 1",
                        borderRadius: "2px",
                        background: MINI_BLUE,
                        border: `1px solid ${MINI_BLUE_BORDER}`,
                    }}
                />
            ))}
        </div>
    );
}

export default function RackReference({ layerCount, boxCount, maxItems }: RackReferenceProps) {
    const visibleLayers = clampVisible(layerCount, LAYER_CAP);
    const visibleBoxes = clampVisible(boxCount, BOX_CAP);

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
                            return (
                                <div key={label} style={cellShell} title={label}>
                                    <MiniItemGrid maxItems={maxItems} />
                                </div>
                            );
                        })}
                    </div>
                ))}
            </div>
        </div>
    );
}
