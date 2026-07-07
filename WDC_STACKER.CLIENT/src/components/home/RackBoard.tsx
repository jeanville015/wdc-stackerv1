import { useEffect, useMemo, useRef, type CSSProperties } from "react";
import RackPanel from "./RackPanel";
import type { CapacityConfig } from "../../types/models";
import type { BoxView } from "../../types/stacker";
import { rackBoardStyle } from "./rackGridStyles";

type RackBoardConfig = Pick<
    CapacityConfig,
    "RACK_COUNT" | "LAYER_COUNT" | "BOX_COUNT" | "MAX_ITEM_PER_BOX"
>;

interface RackBoardProps {
    config: RackBoardConfig;
    boxes?: BoxView[];
    onBoxesChanged: (boxes: BoxView[]) => void;
    recentlyAssignedBoxNo?: string | null;
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

export default function RackBoard({ config, boxes = [], onBoxesChanged, recentlyAssignedBoxNo }: RackBoardProps) {
    const rackCount = Math.max(0, config.RACK_COUNT);
    const layerCount = Math.max(0, config.LAYER_COUNT);
    const boxCount = Math.max(0, config.BOX_COUNT);
    const maxItemPerBox = Math.max(0, config.MAX_ITEM_PER_BOX);
    const rackRefs = useRef<Record<number, HTMLDivElement | null>>({});

    const suggestedTarget = useMemo(
        () => boxes.find((box) => box.IsSuggestedTarget),
        [boxes]
    );

    useEffect(() => {
        if (!suggestedTarget) {
            return;
        }

        const rackElement = rackRefs.current[suggestedTarget.RackNum];

        if (!rackElement) {
            return;
        }

        window.requestAnimationFrame(() => {
            rackElement.scrollIntoView({
                behavior: "smooth",
                block: "center",
                inline: "nearest",
            });
        });
    }, [suggestedTarget?.BoxNo, suggestedTarget?.RackNum]);

    if (rackCount === 0) {
        return <div style={emptyStateStyle}>No racks configured.</div>;
    }

    return (
        <section style={rackBoardStyle}>
            {Array.from({ length: rackCount }, (_, index) => {
                const rackNumber = index + 1;
                const rackBoxes = boxes.filter((box) => box.RackNum === rackNumber);

                return (
                    <div
                        key={rackNumber}
                        ref={(element) => {
                            rackRefs.current[rackNumber] = element;
                        }}
                    >
                        <RackPanel
                            recentlyAssignedBoxNo={recentlyAssignedBoxNo}
                            rackNumber={rackNumber}
                            layerCount={layerCount}
                            boxCount={boxCount}
                            maxItemPerBox={maxItemPerBox}
                            boxes={rackBoxes}
                            onBoxesChanged={onBoxesChanged}
                        />
                    </div>
                );
            })}
        </section>
    );
}