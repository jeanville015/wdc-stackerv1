import type { CSSProperties } from "react";

const GREY_TONES = ["#f8f8f8", "#f1f1f1", "#e8e8e8", "#dfdfdf", "#d6d6d6"];
const GREY_TONE = "#e8e8e8";

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

export const rackSubTitleStyle: CSSProperties = {
    margin: 0,
    color: "#7a869a",
    fontSize: "0.72rem",
    letterSpacing: "0.05em",
};

export const rackScrollStyle: CSSProperties = {
    overflowX: "auto",
    overflowY: "hidden",
    width: "100%",
};

export const rackGridStyle = (boxCount: number, layerCount: number): CSSProperties => ({
    display: "grid",
    gridTemplateColumns: `clamp(96px, 14vw, 140px) repeat(${boxCount}, minmax(48px, 1fr))`,
    gridTemplateRows: `32px repeat(${layerCount}, 48px)`,
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

export const getGreyCellStyle = (): CSSProperties => {
    return {
        ...baseCellStyle,
        width: "100%",
        height: "100%",
        minHeight: "48px",
        background: GREY_TONE,
        boxSizing: "border-box",
    };
};