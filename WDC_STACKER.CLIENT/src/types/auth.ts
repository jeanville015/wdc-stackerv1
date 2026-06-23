// ── Auth types ────────────────────────────────────────────────────────────────
// Shared across the whole app. Import from here, never redefine locally.

/** Credentials the user types into the login form. */
export interface LoginRequest {
    username: string;
    password: string;
}

/** Response shape returned by POST /api/auth/login. */
export interface LoginResponse {
    Success: boolean;
    Token: string;
    Username: string;
    Message: string;
    CanAccessConfiguration: boolean;
}

/** The logged-in user stored in AuthContext / sessionStorage. */
export interface AuthUser {
    username: string;
    token: string;
    canAccessConfiguration: boolean;
}