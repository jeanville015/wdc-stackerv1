import { useState } from "react";
import { Outlet, useNavigate } from "react-router-dom";
import wdLogo from "../assets/wd-logo.png";
import { useAuth } from "../context/useAuth";
import LeftNav from "./LeftNav";

export default function AppShell() {
    const { user, logout } = useAuth();
    const navigate = useNavigate();
    const [isNavigationCollapsed, setIsNavigationCollapsed] = useState(false);

    const handleLogout = () => {
        logout();
        navigate("/login", { replace: true });
    };

    return (
        <div className="stacker-app-shell">
            <LeftNav
                canAccessConfiguration={!!user?.canAccessConfiguration}
                collapsed={isNavigationCollapsed}
                onToggle={() =>
                    setIsNavigationCollapsed((isCollapsed) => !isCollapsed)
                }
            />

            <div className="stacker-app-column">
                <header className="stacker-app-header navbar px-4 px-md-5">
                    <div className="stacker-header-brand">
                        <img
                            className="stacker-header-logo"
                            src={wdLogo}
                            alt="Western Digital"
                        />
                        <span>FGI STACKER</span>
                    </div>

                    <div className="stacker-header-account">
                        <span className="stacker-header-username">
                            {user?.username}
                        </span>

                        <button
                            type="button"
                            className="btn btn-sm stacker-sign-out"
                            onClick={handleLogout}
                        >
                            Sign out
                        </button>
                    </div>
                </header>

                <main className="stacker-main-content">
                    <Outlet />
                </main>
            </div>
        </div>
    );
}
