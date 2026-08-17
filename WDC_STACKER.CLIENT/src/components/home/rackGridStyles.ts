import type { CSSProperties } from "react";
import type { BoxView } from "../../types/stacker";

const GREY_TONE = "#e8e8e8";
const BLUE_LIGHT = "#cfe3ff";
const GREEN_LIGHT = "#d8f3df";

export const TARGET_AMBER = "#f5a300";
export const TARGET_AMBER_DARK = "#b66a00";
export const TARGET_AMBER_TINT = "#fff9eb";

const baseLabelCellStyle: CSSProperties = {
    display: "flex",
    alignItems: "center",
    justifyContent: "center",
    borderRadius: "8px",
    border: "1px solid #dde1e9",
    background: "#ffffff",
    color: "#5e6c84",
    fontSize: "0.72rem",
    fontWeight: 700,
    letterSpacing: "0.04em",
    boxShadow: "0 1px 2px rgba(23,43,77,0.04)",
};

const baseCellStyle: CSSProperties = {
    display: "flex",
    alignItems: "center",
    justifyContent: "center",
    borderRadius: "8px",
    border: "1px solid #cfd5dd",
    boxShadow: "inset 0 1px 0 rgba(255,255,255,0.85), 0 1px 2px rgba(23,43,77,0.06)",
    width: "100%",
    height: "100%",
    minHeight: "48px",
    boxSizing: "border-box",
    overflow: "hidden",
};

export const rackBoardStyle: CSSProperties = {
    display: "flex",
    flexDirection: "column",
    gap: "1rem",
};

export const rackCardStyle: CSSProperties = {
    background: "#ffffff",
    border: "1px solid #dde1e9",
    borderRadius: "14px",
    boxShadow: "0 6px 18px rgba(23,43,77,0.08)",
    padding: "1rem 1rem 1.1rem",
    width: "100%",
    boxSizing: "border-box",
};

export const rackHeaderStyle: CSSProperties = {
    display: "flex",
    alignItems: "flex-end",
    justifyContent: "space-between",
    gap: "1rem",
    marginBottom: "0.9rem",
};

export const rackTitleStyle: CSSProperties = {
    margin: 0,
    color: "#172b4d",
    fontSize: "0.88rem",
    fontWeight: 700,
    letterSpacing: "0.08em",
    textTransform: "uppercase",
};

export const rackScrollStyle: CSSProperties = {
    overflowX: "auto",
    overflowY: "hidden",
    width: "100%",
};

export const rackGridStyle = (boxCount: number, layerCount: number): CSSProperties => ({
    display: "grid",
    gridTemplateColumns: `clamp(96px, 14vw, 140px) repeat(${boxCount}, minmax(72px, 1fr))`,
    gridTemplateRows: `32px repeat(${layerCount}, minmax(56px, auto))`,
    gap: "8px",
    width: "100%",
    minWidth: "100%",
    alignItems: "stretch",
});

export const cornerCellStyle: CSSProperties = {
    background: "transparent",
};

export const columnLabelCellStyle: CSSProperties = {
    ...baseLabelCellStyle,
    width: "100%",
    boxSizing: "border-box",
};

export const rowLabelCellStyle: CSSProperties = {
    ...baseLabelCellStyle,
    width: "100%",
    justifyContent: "flex-start",
    paddingLeft: "0.6rem",
    boxSizing: "border-box",
};

export const getEmptyCellStyle = (): CSSProperties => {
    return {
        ...baseCellStyle,
        background: GREY_TONE,
    };
};

export const getMappedCellStyle = (
    box: BoxView,
    isRecentlyAssigned = false,
    isTarget = false
): CSSProperties => {
    const isHighlighted = isTarget || isRecentlyAssigned;

    return {
        ...baseCellStyle,
        background: isRecentlyAssigned
            ? "#d8f3df"
            : isTarget
                ? TARGET_AMBER_TINT
                : box.HasReleaseStatus
                    ? GREEN_LIGHT
                    : BLUE_LIGHT,
        border: isRecentlyAssigned
            ? "2px solid #16833a"
            : isTarget
                ? `2px solid ${TARGET_AMBER_DARK}`
                : "1px solid #8bbcff",
        padding: "0.25rem",
        outline: "none",
        outlineOffset: "0",
        overflow: isHighlighted ? "visible" : "hidden",
        animation: isRecentlyAssigned ? "assignedBoxPulse 0.9s ease-out infinite" : undefined,
        boxShadow: isRecentlyAssigned
            ? "inset 0 1px 0 rgba(255,255,255,0.85), 0 4px 12px rgba(22,131,58,0.35)"
            : isTarget
                ? `inset 0 1px 0 rgba(255,255,255,0.85), 0 0 0 3px rgba(245,163,0,0.25), 0 4px 10px rgba(245,163,0,0.3)`
                : "inset 0 1px 0 rgba(255,255,255,0.85), 0 1px 2px rgba(23,43,77,0.06)",
    };
};
