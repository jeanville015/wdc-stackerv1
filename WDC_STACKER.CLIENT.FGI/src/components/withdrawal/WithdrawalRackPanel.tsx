import { Fragment, useState, type CSSProperties } from "react";
import type {
    FgiWithdrawalBox,
    FgiWithdrawalRack,
    FgiWithdrawalShipBox,
} from "../../types/withdrawal";
import {
    columnLabelCellStyle,
    cornerCellStyle,
    getEmptyCellStyle,
    rackCardStyle,
    rackHeaderStyle,
    rackOverviewGridStyle,
    rackScrollStyle,
    rackTitleStyle,
    rowLabelCellStyle,
} from "../home/rackGridStyles";
import WithdrawalShipBoxModal from "./WithdrawalShipBoxModal";

interface Props {
    rack: FgiWithdrawalRack;
    rackLayerCount: number;
    rackColumnCount: number;
    shipBoxLayerCount: number;
    shipBoxColumnCount: number;
    maxItemPerShipBox: number;
}

const HOLD_RED = "#d23232";

const miniShipBoxGridStyle = (
    layerCount: number,
    columnCount: number
): CSSProperties => ({
    display: "grid",
    gridTemplateColumns: `repeat(${Math.max(1, columnCount)}, minmax(0, 1fr))`,
    gridTemplateRows: `repeat(${Math.max(1, layerCount)}, minmax(0, 1fr))`,
    gap: "4px",
    width: "74px",
    maxWidth: "100%",
    pointerEvents: "none",
});

const miniShipBoxCellStyle = (
    shipBox: FgiWithdrawalShipBox
): CSSProperties => {
    const isInSiteHold = shipBox.Holders.some(
        (holder) => holder.IsInSiteHold
    );

    return {
        position: "relative",
        aspectRatio: "1 / 1",
        borderRadius: "2px",
        background: isInSiteHold
            ? HOLD_RED
            : shipBox.Holders.length > 0
                ? "#0052cc"
                : "#b7d7ff",
        border: isInSiteHold
            ? "1px solid #a51f1f"
            : "1px solid #8bbcff",
        boxShadow: isInSiteHold
            ? "0 0 0 1px rgba(210,50,50,0.2)"
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
    configuredLayerCount,
    configuredColumnCount,
}: {
    box: FgiWithdrawalBox;
    configuredLayerCount: number;
    configuredColumnCount: number;
}) {
    const layerCount = box.ShipBoxes.reduce(
        (maximum, shipBox) => Math.max(maximum, shipBox.LayerRowNum),
        Math.max(1, configuredLayerCount)
    );
    const columnCount = box.ShipBoxes.reduce(
        (maximum, shipBox) => Math.max(maximum, shipBox.LayerColNum),
        Math.max(1, configuredColumnCount)
    );

    const findShipBox = (layerNumber: number, columnNumber: number) =>
        box.ShipBoxes.find(
            (shipBox) =>
                shipBox.LayerRowNum === layerNumber &&
                shipBox.LayerColNum === columnNumber
        );

    return (
        <span style={miniShipBoxGridStyle(layerCount, columnCount)}>
            {Array.from(
                { length: layerCount * columnCount },
                (_, index) => {
                    const layerNumber = Math.floor(index / columnCount) + 1;
                    const columnNumber = (index % columnCount) + 1;
                    const shipBox = findShipBox(layerNumber, columnNumber);
                    const isInSiteHold = Boolean(
                        shipBox?.Holders.some(
                            (holder) => holder.IsInSiteHold
                        )
                    );

                    return (
                        <span
                            key={
                                shipBox?.ShipBoxName ??
                                `empty-${box.BoxNo}-${layerNumber}-${columnNumber}`
                            }
                            style={
                                shipBox
                                    ? miniShipBoxCellStyle(shipBox)
                                    : miniShipBoxEmptyCellStyle
                            }
                        >
                            {isInSiteHold && (
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
                            )}
                        </span>
                    );
                }
            )}
        </span>
    );
}

export default function WithdrawalRackPanel({
    rack,
    rackLayerCount,
    rackColumnCount,
    shipBoxLayerCount,
    shipBoxColumnCount,
    maxItemPerShipBox,
}: Props) {
    const [selectedBox, setSelectedBox] =
        useState<FgiWithdrawalBox | null>(null);
    const resolvedLayerCount = rack.Boxes.reduce(
        (maximum, box) => Math.max(maximum, box.LayerRowNum),
        Math.max(0, rackLayerCount)
    );
    const resolvedColumnCount = rack.Boxes.reduce(
        (maximum, box) => Math.max(maximum, box.LayerColNum),
        Math.max(0, rackColumnCount)
    );
    const layers = Array.from(
        { length: resolvedLayerCount },
        (_, index) => index + 1
    );
    const columns = Array.from(
        { length: resolvedColumnCount },
        (_, index) => index + 1
    );

    const findBox = (layerNumber: number, columnNumber: number) =>
        rack.Boxes.find(
            (box) =>
                box.LayerRowNum === layerNumber &&
                box.LayerColNum === columnNumber
        );

    return (
        <>
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
                    <h3
                        style={{
                            ...rackTitleStyle,
                            color: "#0b1f55",
                            fontSize: "0.98rem",
                        }}
                    >
                        Rack No. {rack.RackNum}
                    </h3>
                </div>

                <div style={rackScrollStyle}>
                    <div
                        style={rackOverviewGridStyle(
                            columns.length,
                            layers.length
                        )}
                    >
                        <div
                            style={{
                                ...cornerCellStyle,
                                display: "flex",
                                alignItems: "center",
                                color: "#0b1f55",
                                fontSize: "0.7rem",
                                fontWeight: 800,
                                textTransform: "uppercase",
                            }}
                        >
                            Layer
                        </div>

                        {columns.map((columnNumber) => (
                            <div
                                key={`withdrawal-column-${columnNumber}`}
                                style={{
                                    ...columnLabelCellStyle,
                                    border: 0,
                                    background: "transparent",
                                    boxShadow: "none",
                                    color: "#0b1f55",
                                    fontSize: "0.76rem",
                                }}
                            >
                                {String(columnNumber).padStart(2, "0")}
                            </div>
                        ))}

                        {layers.map((layerNumber) => (
                            <Fragment key={`withdrawal-layer-${layerNumber}`}>
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
                                    const box = findBox(
                                        layerNumber,
                                        columnNumber
                                    );
                                    const hasShipBoxes = Boolean(
                                        box?.ShipBoxes.length
                                    );
                                    const heldHolders = box
                                        ? box.ShipBoxes.flatMap((shipBox) =>
                                            shipBox.Holders.filter(
                                                (holder) => holder.IsInSiteHold
                                            )
                                        )
                                        : [];

                                    return (
                                        <button
                                            type="button"
                                            key={`withdrawal-${layerNumber}-${columnNumber}`}
                                            disabled={!hasShipBoxes}
                                            onClick={() => {
                                                if (box && hasShipBoxes)
                                                    setSelectedBox(box);
                                            }}
                                            style={{
                                                ...(box
                                                    ? {
                                                        ...getEmptyCellStyle(),
                                                        minHeight: "128px",
                                                        background: "#ffffff",
                                                        border: "1px solid #d9e2ef",
                                                        padding: "0.5rem",
                                                        boxShadow: "0 1px 3px rgba(23,43,77,0.08)",
                                                    }
                                                    : getEmptyCellStyle()),
                                                position: "relative",
                                                cursor: hasShipBoxes
                                                    ? "pointer"
                                                    : "default",
                                            }}
                                            aria-label={
                                                box
                                                    ? `Open ShipBoxes for ${box.BoxNo}${heldHolders.length > 0 ? `, ${heldHolders.length} holders on in-site hold` : ""}`
                                                    : "Empty rack cell"
                                            }
                                        >
                                            {box && (
                                                <>
                                                    <span className="rack-box-content">
                                                        <span className="rack-box-name">
                                                            {box.BoxNo}
                                                        </span>

                                                        <MiniShipBoxGrid
                                                            box={box}
                                                            configuredLayerCount={
                                                                shipBoxLayerCount
                                                            }
                                                            configuredColumnCount={
                                                                shipBoxColumnCount
                                                            }
                                                        />
                                                    </span>

                                                    {heldHolders.length > 0 && (
                                                        <span className="rack-box-in-site-hold-badge">
                                                            IN-SITE
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
            </article>

            {selectedBox && (
                <WithdrawalShipBoxModal
                    box={selectedBox}
                    layerCount={shipBoxLayerCount}
                    columnCount={shipBoxColumnCount}
                    maxItemPerShipBox={maxItemPerShipBox}
                    onClose={() => setSelectedBox(null)}
                />
            )}
        </>
    );
}