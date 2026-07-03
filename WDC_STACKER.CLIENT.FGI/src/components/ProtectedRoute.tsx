import { Navigate } from "react-router-dom";
import { useAuth } from "../context/AuthContext";
import type { ReactNode } from "react";

interface Props {
    children: ReactNode;
    requireConfigurationAccess?: boolean;
}

/**
 * Wrap any route element with <ProtectedRoute> to require authentication.
 * If the user is not logged in they are sent back to /login.
 */
export default function ProtectedRoute({ children, requireConfigurationAccess = false }: Props) {
    const { user } = useAuth();

    if (!user) return <Navigate to="/login" replace />;

    if (requireConfigurationAccess && !user.canAccessConfiguration) {
        return <Navigate to="/" replace />;
    }

    return <>{children}</>;
}