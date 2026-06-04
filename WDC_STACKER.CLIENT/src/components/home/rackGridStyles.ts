import type { CSSProperties } from "react";
import type { BoxView } from "../../types/stacker";

const GREY_TONE = "#e8e8e8";
const BLUE_DARK = "#0052cc";
const BLUE_LIGHT = "#cfe3ff";
const BLUE_BORDER = "#003d99";

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

export const getMappedCellStyle = (box: BoxView): CSSProperties => {
    const percentage = Math.min(Math.max(Number(box.BoxListPercentage), 0), 100);

    return {
        ...baseCellStyle,
        background: `linear-gradient(90deg, ${BLUE_DARK} 0%, ${BLUE_DARK} ${percentage}%, ${BLUE_LIGHT} ${percentage}%, ${BLUE_LIGHT} 100%)`,
        border: box.IsSuggestedTarget
            ? `2px solid ${BLUE_BORDER}`
            : "1px solid #8bbcff",
        padding: "0.25rem",
        outline: box.IsSuggestedTarget
            ? `2px solid ${BLUE_BORDER}`
            : "none",
        outlineOffset: box.IsSuggestedTarget ? "-5px" : "0",
        boxShadow: box.IsSuggestedTarget
            ? `inset 0 0 0 2px #ffffff, 0 0 0 2px ${BLUE_BORDER}, 0 4px 10px rgba(0,82,204,0.35)`
            : "inset 0 1px 0 rgba(255,255,255,0.85), 0 1px 2px rgba(23,43,77,0.06)",
    };
};