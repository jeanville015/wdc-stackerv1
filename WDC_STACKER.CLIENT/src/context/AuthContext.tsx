import { useState, type ReactNode } from "react";
import { AuthContext } from "./authContextValue";
import type { AuthUser } from "../types/auth";

// ── Context ───────────────────────────────────────────────────────────────────

// ── Provider ──────────────────────────────────────────────────────────────────
export function AuthProvider({ children }: { children: ReactNode }) {
    const [user, setUser] = useState<AuthUser | null>(() => {
        // Re-hydrate from sessionStorage so a page refresh keeps the user logged in
        const raw = sessionStorage.getItem("auth_user");
        return raw ? (JSON.parse(raw) as AuthUser) : null;
    });

    const login = (u: AuthUser) => {
        sessionStorage.setItem("auth_user", JSON.stringify(u));
        setUser(u);
    };

    const logout = () => {
        sessionStorage.removeItem("auth_user");
        setUser(null);
    };

    return (
        <AuthContext.Provider value={{ user, login, logout }}>
            {children}
        </AuthContext.Provider>
    );
}

// ── Hook ──────────────────────────────────────────────────────────────────────
