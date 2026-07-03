// ── API config ────────────────────────────────────────────────────────────────
//
// Your vite.config.ts already proxies every /api/* request to
// http://localhost:5002, so we never need a hostname or port here.
//
// During development:
//   fetch("/api/auth/login")  →  Vite proxy  →  http://localhost:5002/api/auth/login
//
// In production (deployed):
//   Set VITE_API_BASE_URL in your build environment if the API is on a
//   different origin. Leave it unset to keep using relative paths.

export const API_BASE: string =
    import.meta.env.VITE_API_BASE_URL?.replace(/\/$/, "") ?? "";

// Usage:  `${API_BASE}/api/auth/login`
// Dev:    ""   + "/api/auth/login"  →  "/api/auth/login"  (proxied by Vite)
// Prod:   "https://wdc-api.example.com" + "/api/auth/login"