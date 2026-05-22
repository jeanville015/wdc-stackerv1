import { useAuth } from "../context/AuthContext";
import { useNavigate } from "react-router-dom";
import LeftNav from "../components/LeftNav";

export default function HomePage() {
    const { user, logout } = useAuth();
    const navigate = useNavigate();

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
                <LeftNav />

                {/* Main content area */}
                <main
                    style={{
                        flex: 1,
                        padding: "2rem 2.5rem",
                        overflowY: "auto",
                        background: "#f4f5f7",
                    }}
                >
                    {/* Blank — content will be added here */}
                </main>

            </div>
        </div>
    );
}