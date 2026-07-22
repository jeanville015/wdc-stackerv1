import type { CSSProperties } from "react";
import type { BoxView } from "../../types/stacker";

const SEGMENT_CAP = 10;
const BLUE_DARK = "#0052cc";
const BLUE_LIGHT = "#cfe3ff";
const GREEN_DARK = "#16833a";
const GREEN_LIGHT = "#d8f3df";

interface SegmentedBoxProps {
    box: BoxView;
    maxItemPerBox: number;
}

export default function SegmentedBox({ box, maxItemPerBox }: SegmentedBoxProps) {
    const configuredCapacity = Math.max(1, Number(maxItemPerBox) || 1);
    const visibleCapacity = Math.min(configuredCapacity, SEGMENT_CAP);
    const itemCount = Math.max(0, Number(box.BoxListCount) || 0);
    const occupiedSegments = itemCount === 0
        ? 0
        : Math.min(
            visibleCapacity,
            Math.max(1, Math.round((itemCount / configuredCapacity) * visibleCapacity))
        );
    const isRelease = box.HasReleaseStatus;
    const filledColor = isRelease ? GREEN_DARK : BLUE_DARK;
    const availableColor = isRelease ? GREEN_LIGHT : BLUE_LIGHT;

    const trackStyle: CSSProperties = {
        position: "absolute",
        inset: "4px",
        display: "grid",
        gridTemplateColumns: `repeat(${visibleCapacity}, minmax(0, 1fr))`,
        gap: "2px",
        padding: "3px",
        borderRadius: "6px",
        background: availableColor,
        boxSizing: "border-box",
    };

    return (
        <span
            aria-hidden="true"
            style={{
                position: "absolute",
                inset: 0,
                overflow: "hidden",
                pointerEvents: "none",
            }}
        >
            <span style={trackStyle}>
                {Array.from({ length: visibleCapacity }, (_, index) => (
                    <span
                        key={index}
                        style={{
                            minWidth: 0,
                            borderRadius: "3px",
                            background: index < occupiedSegments ? filledColor : availableColor,
                            boxShadow: "inset 0 1px 0 rgba(255,255,255,0.32)",
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
                    padding: "0.2rem",
                    color: occupiedSegments > 0 ? "#ffffff" : "#172b4d",
                    fontSize: "0.66rem",
                    fontWeight: 800,
                    lineHeight: 1.05,
                    textAlign: "center",
                    textShadow: occupiedSegments > 0 ? "0 1px 2px rgba(0,0,0,0.55)" : undefined,
                    overflowWrap: "anywhere",
                }}
            >
                <span>{box.BoxNo}</span>
                <small>{itemCount}/{configuredCapacity}</small>
            </span>
        </span>
    );
}
