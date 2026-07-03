import { createContext, useContext, useState, type ReactNode } from "react";
import type { AuthUser } from "../types/auth";

interface AuthContextValue {
    user: AuthUser | null;
    login: (user: AuthUser) => void;
    logout: () => void;
}

// ── Context ───────────────────────────────────────────────────────────────────
const AuthContext = createContext<AuthContextValue | null>(null);

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
export function useAuth(): AuthContextValue {
    const ctx = useContext(AuthContext);
    if (!ctx) throw new Error("useAuth must be used inside <AuthProvider>");
    return ctx;
}