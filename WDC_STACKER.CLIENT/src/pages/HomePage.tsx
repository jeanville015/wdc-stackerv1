import { useState, type CSSProperties } from "react";
import StackerOperationControls from "../components/StackerOperationControls";
import RackBoard from "../components/home/RackBoard";
import { useCapacityConfig } from "../hooks/useCapacityConfig";
import type { BoxView } from "../types/stacker";

const shellStyle: CSSProperties = {
    minHeight: "100vh",
    background: "#f4f5f7",
    display: "flex",
    flexDirection: "column",
};

const mainStyle: CSSProperties = {
    flex: 1,
    minWidth: 0,
    padding: "1.75rem 2rem",
    overflowY: "auto",
    overflowX: "hidden",
    background: "#f4f5f7",
};

const pageHeaderStyle: CSSProperties = {
    marginBottom: "1.1rem",
};

const pageTitleStyle: CSSProperties = {
    margin: 0,
    color: "#172b4d",
    fontSize: "1.35rem",
    fontWeight: 700,
    letterSpacing: "-0.02em",
};

const pageSubtitleStyle: CSSProperties = {
    marginTop: "0.35rem",
    marginBottom: 0,
    color: "#5e6c84",
    fontSize: "0.84rem",
    letterSpacing: "0.03em",
};

const panelStyle: CSSProperties = {
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
}; 

export default function HomePage() {  
    const { config, loading, error } = useCapacityConfig();
    const [gridViewBoxes, setGridViewBoxes] = useState<BoxView[]>([]);

    return (
        <div style={{ display: "flex", gap: "1rem", alignItems: "flex-start" }}>
            <StackerOperationControls onGridViewBoxesLoaded={setGridViewBoxes} />

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
                    />
                )}
            </section>
        </div>
    );
}