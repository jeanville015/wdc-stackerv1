import { useEffect, useRef, useState } from "react";
import StackerOperationControls from "../components/StackerOperationControls";
import RackBoard from "../components/home/RackBoard";
import { useCapacityConfig } from "../hooks/useCapacityConfig";
import type { BoxView } from "../types/stacker";

export default function HomePage() {  
    const { config, loading, error } = useCapacityConfig();
    const [gridViewBoxes, setGridViewBoxes] = useState<BoxView[]>([]);
    const [selectedTargetBox, setSelectedTargetBox] = useState<BoxView | null>(null);
    const [boxSelectionEnabled, setBoxSelectionEnabled] = useState(false);
    const [recentlyAssignedBoxNo, setRecentlyAssignedBoxNo] = useState<string | null>(null);
    const assignedBoxTimerRef = useRef<number | null>(null);

    useEffect(() => {
        return () => {
            if (assignedBoxTimerRef.current) {
                window.clearTimeout(assignedBoxTimerRef.current);
            }
        };
    }, []);

    const showAssignedBoxConfirmation = (boxNo: string) => {
        setRecentlyAssignedBoxNo(boxNo);

        if (assignedBoxTimerRef.current) {
            window.clearTimeout(assignedBoxTimerRef.current);
        }

        assignedBoxTimerRef.current = window.setTimeout(() => {
            setRecentlyAssignedBoxNo(null);
        }, 4000);
    };

    return (
        <div style={{ display: "flex", gap: "1rem", alignItems: "flex-start" }}>
            <StackerOperationControls
                onGridViewBoxesLoaded={setGridViewBoxes}
                selectedTargetBox={selectedTargetBox}
                onSelectedTargetBoxChanged={setSelectedTargetBox}
                onBoxSelectionEnabledChanged={setBoxSelectionEnabled}
                onAssignedBoxConfirmed={showAssignedBoxConfirmation}
            />

            <section style={{ flex: 1, minWidth: 0 }}>
                {loading && (
                    <div
                        style={{
                            background: "#ffffff",
                            border: "1px solid #dde1e9",
                            borderRadius: "12px",
                            boxShadow: "0 4px 18px rgba(23,43,77,0.08)",
                            padding: "1rem 1.1rem",
                            color: "#172b4d",
                            display: "flex",
                            alignItems: "center",
                            gap: "0.75rem",
                            fontSize: "0.88rem",
                        }}
                    >
                        <span
                            className="spinner-border spinner-border-sm"
                            role="status"
                            aria-hidden="true"
                        />
                        Loading capacity configuration...
                    </div>
                )}

                {!loading && error && (
                    <div
                        role="alert"
                        style={{
                            background: "#ffffff",
                            border: "1px solid #dde1e9",
                            borderLeft: "3px solid #d23232",
                            borderRadius: "12px",
                            boxShadow: "0 4px 18px rgba(23,43,77,0.08)",
                            padding: "1rem 1.1rem",
                            color: "#d23232",
                            fontSize: "0.88rem",
                        }}
                    >
                        {error}
                    </div>
                )}

                {!loading && !error && config && (
                    <RackBoard
                        config={{
                            RACK_COUNT: config.RACK_COUNT,
                            LAYER_COUNT: config.LAYER_COUNT,
                            BOX_COUNT: config.BOX_COUNT,
                            MAX_ITEM_PER_BOX: config.MAX_ITEM_PER_BOX,
                        }}
                        boxes={gridViewBoxes}
                        onBoxesChanged={setGridViewBoxes}
                        boxSelectionEnabled={boxSelectionEnabled}
                        selectedTargetBox={selectedTargetBox}
                        recentlyAssignedBoxNo={recentlyAssignedBoxNo}
                        onTargetBoxSelected={(box) => {
                            setSelectedTargetBox(box);
                            setBoxSelectionEnabled(false);
                        }}
                    />
                )}
            </section>
        </div>
    );
}