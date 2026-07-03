import { API_BASE } from "../config/apiConfig";
import type { LoginRequest, LoginResponse } from "../types/auth";

/**
 * POST /api/auth/login
 * Sends credentials to the Web API and returns the login result.
 * Vite proxies /api/* to http://localhost:5002 in development.
 */
export async function loginApi(request: LoginRequest): Promise<LoginResponse> {
    const response = await fetch(`${API_BASE}/api/auth/login`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(request),
    });

    if (!response.ok) {
        const err = await response.json().catch(() => ({}));
        throw new Error((err as { message?: string }).message ?? "Login failed.");
    }

    return response.json() as Promise<LoginResponse>;
}
