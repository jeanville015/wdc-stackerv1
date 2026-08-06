import { NavLink } from "react-router-dom";
import wdLogo from "../assets/wd-logo.png";

interface LeftNavProps {
    canAccessConfiguration: boolean;
    collapsed: boolean;
    onToggle: () => void;
}

const getLinkClassName = ({ isActive }: { isActive: boolean }) =>
    ["stacker-side-nav-link", isActive ? "is-active" : ""]
        .filter(Boolean)
        .join(" ");

export default function LeftNav({
    canAccessConfiguration,
    collapsed,
    onToggle,
}: LeftNavProps) {
    return (
        <aside
            className={[
                "stacker-side-nav",
                collapsed ? "is-collapsed" : "is-expanded",
            ].join(" ")}
        >
            <div className="stacker-side-nav-brand">
                {collapsed ? (
                    <img
                        className="stacker-side-nav-logo"
                        src={wdLogo}
                        alt="Western Digital"
                    />
                ) : (
                    <h2>FGI STACKER</h2>
                )}
            </div>

            <nav
                id="stacker-primary-navigation"
                className="stacker-side-nav-links"
                aria-label="Main navigation"
            >
                {!collapsed && (
                    <p className="stacker-side-nav-label">Navigation</p>
                )}

                <NavLink
                    to="/"
                    end
                    className={getLinkClassName}
                    data-label="Home"
                    aria-label={collapsed ? "Home" : undefined}
                >
                    <i className="fa-solid fa-barcode" aria-hidden="true" />
                    <span>Home</span>
                </NavLink>

                {canAccessConfiguration && (
                    <NavLink
                        to="/config"
                        className={getLinkClassName}
                        data-label="Configuration"
                        aria-label={collapsed ? "Configuration" : undefined}
                    >
                        <i className="fa-solid fa-gear" aria-hidden="true" />
                        <span>Configuration</span>
                    </NavLink>
                )}
            </nav>

            <button
                type="button"
                className="stacker-side-nav-toggle"
                onClick={onToggle}
                aria-controls="stacker-primary-navigation"
                aria-expanded={!collapsed}
                aria-label={
                    collapsed
                        ? "Expand main navigation"
                        : "Collapse main navigation"
                }
                title={collapsed ? "Expand navigation" : "Collapse navigation"}
            >
                <i
                    className={`fa-solid fa-chevron-${collapsed ? "right" : "left"}`}
                    aria-hidden="true"
                />
            </button>
        </aside>
    );
}