import { useState, type ReactNode } from "react";
import type { AuthUser } from "../types/auth";
import { AuthContext } from "./AuthContext";

export function AuthProvider({ children }: { children: ReactNode }) {
    const [user, setUser] = useState<AuthUser | null>(() => {
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
