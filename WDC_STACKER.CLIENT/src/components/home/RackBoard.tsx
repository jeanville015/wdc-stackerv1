import { type CSSProperties } from "react";
import RackPanel from "./RackPanel";
import type { CapacityConfig } from "../../types/models";
import type { BoxView } from "../../types/stacker";
import { rackBoardStyle } from "./rackGridStyles";

type RackBoardConfig = Pick<CapacityConfig, "RACK_COUNT" | "LAYER_COUNT" | "BOX_COUNT">;

interface RackBoardProps {
    config: RackBoardConfig;
    boxes?: BoxView[];
}

const emptyStateStyle: CSSProperties = {
    background: "#ffffff",
    border: "1px solid #dde1e9",
    borderRadius: "12px",
    boxShadow: "0 4px 18px rgba(23,43,77,0.08)",
    padding: "1rem 1.1rem",
    color: "#5e6c84",
    fontSize: "0.88rem",
};

export default function RackBoard({ config, boxes = [] }: RackBoardProps) {
    const rackCount = Math.max(0, config.RACK_COUNT);
    const layerCount = Math.max(0, config.LAYER_COUNT);
    const boxCount = Math.max(0, config.BOX_COUNT);

    if (rackCount === 0) {
        return <div style={emptyStateStyle}>No racks configured.</div>;
    }

    return (
        <section style={rackBoardStyle}>
            {Array.from({ length: rackCount }, (_, index) => {
                const rackNumber = index + 1;
                const rackBoxes = boxes.filter((box) => box.RackNum === rackNumber);

                return (
                    <RackPanel
                        key={rackNumber}
                        rackNumber={rackNumber}
                        layerCount={layerCount}
                        boxCount={boxCount}
                        boxes={rackBoxes}
                    />
                );
            })}
        </section>
    );
}