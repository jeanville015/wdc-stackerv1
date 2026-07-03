import { NavLink } from "react-router-dom";
import type { CSSProperties } from "react";

interface LeftNavProps {
    canAccessConfiguration: boolean;
};

const navStyle: CSSProperties = {
    width: "220px",
    minWidth: "220px",
    backgroundColor: "#003d99",
    backgroundImage: `
        linear-gradient(160deg, #0052cc 0%, #003d99 100%),
        linear-gradient(rgba(255,255,255,0.07) 1px, transparent 1px),
        linear-gradient(90deg, rgba(255,255,255,0.07) 1px, transparent 1px)
    `,
    backgroundSize: "auto, 28px 28px, 28px 28px",
    borderRight: "1px solid rgba(255,255,255,0.14)",
    padding: "1.25rem 0.85rem",
    display: "flex",
    flexDirection: "column",
    gap: "1rem",
    color: "#ffffff",
};

const brandBlockStyle: CSSProperties = {
    padding: "0.35rem 0.25rem 0.85rem",
};

const badgeStyle: CSSProperties = {
    display: "inline-block",
    fontSize: "0.58rem",
    letterSpacing: "0.18em",
    textTransform: "uppercase",
    color: "#00c5ff",
    border: "1px solid rgba(0,197,255,0.35)",
    padding: "0.2rem 0.45rem",
    borderRadius: "2px",
    marginBottom: "0.65rem",
};

const titleStyle: CSSProperties = {
    margin: 0,
    color: "#ffffff",
    fontSize: "0.95rem",
    fontWeight: 800,
    letterSpacing: "0.08em",
};

const accentStyle: CSSProperties = {
    width: "34px",
    height: "2px",
    background: "#00c5ff",
    borderRadius: "2px",
    marginTop: "0.55rem",
};

const navGroupStyle: CSSProperties = {
    display: "flex",
    flexDirection: "column",
    gap: "0.45rem",
};

const navTitleStyle: CSSProperties = {
    margin: 0,
    padding: "0 0.25rem",
    color: "rgba(255,255,255,0.48)",
    fontSize: "0.62rem",
    fontWeight: 700,
    letterSpacing: "0.14em",
    textTransform: "uppercase",
};

const getLinkStyle = ({ isActive }: { isActive: boolean }): CSSProperties => ({
    display: "block",
    padding: "0.65rem 0.75rem",
    borderRadius: "7px",
    textDecoration: "none",
    fontSize: "0.82rem",
    fontWeight: 700,
    color: isActive ? "#ffffff" : "rgba(255,255,255,0.72)",
    background: isActive ? "rgba(0,197,255,0.18)" : "rgba(255,255,255,0.055)",
    border: isActive
        ? "1px solid rgba(0,197,255,0.42)"
        : "1px solid rgba(255,255,255,0.08)",
    boxShadow: isActive ? "0 0 10px rgba(0,197,255,0.16)" : "none",
});

export default function LeftNav({ canAccessConfiguration }: LeftNavProps) {
    return (
        <aside style={navStyle}>
            <div style={brandBlockStyle}>
                <span style={badgeStyle}>Western Digital</span>
                <h2 style={titleStyle}>PWD STACKER</h2>
                <div style={accentStyle} />
            </div>

            <nav style={navGroupStyle} aria-label="Main navigation">
                <p style={navTitleStyle}>Navigation</p>

                <NavLink to="/" style={getLinkStyle}>
                    Home
                </NavLink>

                {canAccessConfiguration && (
                    <NavLink to="/config" style={getLinkStyle}>
                        Configuration
                    </NavLink>
                )}
            </nav>
        </aside>
    );
}