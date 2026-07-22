import { type CSSProperties } from "react";
import RackPanel from "./RackPanel";
import type { CapacityConfig } from "../../types/models";
import type { BoxView, ShipBoxView } from "../../types/stacker";
import { rackBoardStyle } from "./rackGridStyles";

type RackBoardConfig = Pick<
    CapacityConfig,
    | "RACK_COUNT"
    | "LAYER_COUNT"
    | "BOX_COUNT"
    | "MAX_ITEM_PER_BOX"
    | "LAYER_COUNT-SHIPBOX"
    | "BOX_COUNT-SHIPBOX"
    | "MAX_ITEM_PER_BOX-SHIPBOX"
>;

interface RackBoardProps {
    config: RackBoardConfig;
    boxes?: BoxView[];
    boxSelectionEnabled: boolean;
    selectedTargetBox: BoxView | null;
    selectedTargetShipBox: ShipBoxView | null;
    recentlyAssignedBoxNo?: string | null;
    onTargetShipBoxSelected: (box: BoxView, shipBox: ShipBoxView) => void;
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

export default function RackBoard({
    config,
    boxes = [],
    boxSelectionEnabled,
    selectedTargetBox,
    selectedTargetShipBox,
    onTargetShipBoxSelected,
    recentlyAssignedBoxNo,
}: RackBoardProps) {
    const rackCount = Math.max(0, config.RACK_COUNT);
    const layerCount = Math.max(0, config.LAYER_COUNT);
    const boxCount = Math.max(0, config.BOX_COUNT); 

    const shipBoxLayerCount = Math.max(0, config["LAYER_COUNT-SHIPBOX"]);
    const shipBoxBoxCount = Math.max(0, config["BOX_COUNT-SHIPBOX"]);
    const maxItemPerShipBox = Math.max(0, config["MAX_ITEM_PER_BOX-SHIPBOX"]);

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
                        recentlyAssignedBoxNo={recentlyAssignedBoxNo}
                        rackNumber={rackNumber}
                        layerCount={layerCount}
                        boxCount={boxCount} 
                        shipBoxLayerCount={shipBoxLayerCount}
                        shipBoxBoxCount={shipBoxBoxCount}
                        maxItemPerShipBox={maxItemPerShipBox}
                        boxes={rackBoxes}
                        boxSelectionEnabled={boxSelectionEnabled}
                        selectedTargetBox={selectedTargetBox}
                        selectedTargetShipBox={selectedTargetShipBox}
                        onTargetShipBoxSelected={onTargetShipBoxSelected}
                    />
                );
            })}
        </section>
    );
}
