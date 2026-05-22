import { Navigate } from "react-router-dom";
import { useAuth } from "../context/AuthContext";
import type { ReactNode } from "react";

interface Props {
    children: ReactNode;
}

/**
 * Wrap any route element with <ProtectedRoute> to require authentication.
 * If the user is not logged in they are sent back to /login.
 */
export default function ProtectedRoute({ children }: Props) {
    const { user } = useAuth();
    return user ? <>{children}</> : <Navigate to="/login" replace />;
}
