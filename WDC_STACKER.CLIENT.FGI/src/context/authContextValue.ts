import { createContext } from "react";
import type { AuthUser } from "../types/auth";

export interface AuthContextValue {
    user: AuthUser | null;
    login: (user: AuthUser) => void;
    logout: () => void;
}

export const AuthContext = createContext<AuthContextValue | null>(null);
