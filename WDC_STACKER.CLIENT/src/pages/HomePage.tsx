import { useState, type CSSProperties } from "react";
import { useNavigate } from "react-router-dom";
import LeftNav from "../components/LeftNav";
import RackBoard from "../components/home/RackBoard";
import { useAuth } from "../context/AuthContext";
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

const errorPanelStyle: CSSProperties = {
    ...panelStyle,
    borderLeft: "3px solid #d23232",
    color: "#d23232",
};


export default function HomePage() {
    const { user, logout } = useAuth();
    const navigate = useNavigate();
    const { config, loading, error } = useCapacityConfig();
    const [gridViewBoxes, setGridViewBoxes] = useState<BoxView[]>([]);

    const handleLogout = () => {
        logout();
        navigate("/login", { replace: true });
    };

    return (
        <div
            style={{
                minHeight: "100vh", background: "#f4f5f7",
                display: "flex", flexDirection: "column"
            }}
        >
            {/* ── Top navbar ── */}
            <nav
                className="navbar px-4 px-md-5"
                style={{
                    background: "#ffffff",
                    borderBottom: "1px solid #dde1e9",
                    height: "60px",
                    boxShadow: "0 1px 6px rgba(23,43,77,0.07)",
                    flexShrink: 0,
                }}
            >
                {/* Brand */}
                <div className="d-flex align-items-center gap-2">
                    <div
                        style={{
                            width: "30px", height: "30px", borderRadius: "6px",
                            background: "linear-gradient(135deg, #0052cc, #00c5ff)",
                            display: "flex", alignItems: "center", justifyContent: "center",
                        }}
                    >
                        <span style={{
                            color: "#fff", fontSize: "0.6rem",
                            fontWeight: 700, letterSpacing: "0.05em"
                        }}>
                            WDC
                        </span>
                    </div>
                    <span
                        className="fw-bold"
                        style={{
                            color: "#172b4d", fontSize: "1rem",
                            letterSpacing: "0.06em"
                        }}
                    >
                        STACKER
                    </span>
                </div>

                {/* Right */}
                <div className="d-flex align-items-center gap-3">
                    <span style={{ fontSize: "0.78rem", color: "#5e6c84", fontWeight: 500 }}>
                        {user?.username}
                    </span>
                    <button
                        className="btn btn-sm"
                        onClick={handleLogout}
                        style={{
                            fontSize: "0.72rem", letterSpacing: "0.1em",
                            textTransform: "uppercase", fontWeight: 600,
                            color: "#0052cc", border: "1.5px solid #0052cc",
                            borderRadius: "6px", padding: "0.3rem 0.85rem",
                            background: "transparent", transition: "all 0.15s",
                        }}
                    >
                        Sign out
                    </button>
                </div>
            </nav>

            {/* ── Body: left nav + main ── */}
            <div style={{ display: "flex", flex: 1, overflow: "hidden" }}>

                {/* Left navigation pane */}
                <LeftNav onGridViewBoxesLoaded={setGridViewBoxes} />

                {/* Main content area */}
                <main
                    style={{
                        flex: 1,
                        padding: "2rem 2.5rem",
                        overflowY: "auto",
                        background: "#f4f5f7",
                    }}
                >
                    <div style={{ marginBottom: "1.1rem" }}>
                        <h2
                            style={{
                                margin: 0,
                                color: "#172b4d",
                                fontSize: "1.35rem",
                                fontWeight: 700,
                                letterSpacing: "-0.02em",
                            }}
                        >
                            {/* In-page title here */} 
                        </h2>
                        <p
                            style={{
                                marginTop: "0.35rem",
                                marginBottom: 0,
                                color: "#5e6c84",
                                fontSize: "0.84rem",
                                letterSpacing: "0.03em",
                            }}
                        >
                            {/* In-page title details*/} 
                        </p>
                    </div>

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
                            }}
                            boxes={gridViewBoxes}
                        />
                    )}
                </main>

            </div>
        </div>
    );
}