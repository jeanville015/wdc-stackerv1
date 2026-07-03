import { useState, type FormEvent, type CSSProperties } from "react";
import { useNavigate } from "react-router-dom";
import { useAuth } from "../context/AuthContext";
import { loginApi } from "../api/AuthApi";

// ── Decorative grid of embossed/active buttons ────────────────────────────────
// Seeded random so the pattern is stable across renders.
function seededRandom(seed: number) {
    const x = Math.sin(seed + 1) * 10000;
    return x - Math.floor(x);
}

type CellVariant = "active" | "bold" | "emboss" | "ghost";

function getVariant(index: number): CellVariant {
    const r = seededRandom(index);
    if (r < 0.12) return "active";   // cyan filled
    if (r < 0.28) return "bold";     // white border + glow
    if (r < 0.50) return "emboss";   // raised white
    return "ghost";                   // near-invisible
}

const cellStyles: Record<CellVariant, CSSProperties> = {
    active: {
        background: "rgba(0,197,255,0.22)",
        border: "1.5px solid rgba(0,197,255,0.7)",
        boxShadow: "0 0 10px rgba(0,197,255,0.35), inset 0 1px 0 rgba(255,255,255,0.2)",
        borderRadius: "5px",
    },
    bold: {
        background: "rgba(255,255,255,0.08)",
        border: "1.5px solid rgba(255,255,255,0.45)",
        boxShadow: "0 2px 8px rgba(0,0,0,0.25), inset 0 1px 0 rgba(255,255,255,0.15)",
        borderRadius: "5px",
    },
    emboss: {
        background: "rgba(255,255,255,0.05)",
        border: "1px solid rgba(255,255,255,0.12)",
        boxShadow: "inset 0 1px 0 rgba(255,255,255,0.10), 0 1px 3px rgba(0,0,0,0.2)",
        borderRadius: "5px",
    },
    ghost: {
        background: "transparent",
        border: "1px solid rgba(255,255,255,0.04)",
        borderRadius: "5px",
    },
};

function DecorativeGrid() {
    const COLS = 9;
    const ROWS = 14;
    const cells = Array.from({ length: COLS * ROWS }, (_, i) => i);

    return (
        <div
            aria-hidden="true"
            style={{
                position: "absolute",
                inset: 0,
                display: "grid",
                gridTemplateColumns: `repeat(${COLS}, 1fr)`,
                gridTemplateRows: `repeat(N, 1fr)`,
                gap: "6px",
                padding: "20px",
                alignContent: "none",
                pointerEvents: "none",
                opacity: 0.9,
            }}
        >
            {cells.map((i) => {
                const variant = getVariant(i);
                return (
                    <div
                        key={i}
                        style={{
                            ...cellStyles[variant],
                            transition: "opacity 0.2s",
                            height: "none",
                        }}
                    />
                );
            })}
        </div>
    );
}

// ── Main page ─────────────────────────────────────────────────────────────────
export default function LoginPage() {
    const { login } = useAuth();
    const navigate = useNavigate();

    const [username, setUsername] = useState("");
    const [password, setPassword] = useState("");
    const [error, setError] = useState<string | null>(null);
    const [loading, setLoading] = useState(false);

    const handleSubmit = async (e: FormEvent) => {
        e.preventDefault();
        setError(null);

        if (!username.trim() || !password.trim()) {
            setError("Username and password are required.");
            return;
        }

        setLoading(true);
        try {
            const result = await loginApi({ username: username.trim(), password });
            if (result.Success) {
                login({ username: result.Username, token: result.Token, canAccessConfiguration: result.CanAccessConfiguration, });
                navigate("/", { replace: true });
            } else {
                setError(result.Message || "Login failed.");
            }
        } catch (err) {
            setError(err instanceof Error ? err.message : "Unexpected error.");
        } finally {
            setLoading(false);
        }
    };

    return (
        <div
            className="container-fluid min-vh-100 d-flex p-0"
            style={{ background: "#f4f5f7" }}
        >
            {/* ── Left brand panel ── */}
            <div
                className="d-none d-md-flex flex-column justify-content-between p-5"
                style={{
                    width: "42%",
                    background: "linear-gradient(160deg, #0052cc 0%, #003d99 100%)",
                    position: "relative",
                    overflow: "hidden",
                }}
            >
                {/* Decorative embossed button grid */}
                <DecorativeGrid />

                {/* Content sits above grid via z-index */}
                {/* Top badge */}
                <div style={{ position: "relative", zIndex: 2 }}>
                    <span
                        className="fw-semibold"
                        style={{
                            fontSize: "0.65rem", letterSpacing: "0.3em",
                            textTransform: "uppercase", color: "#00c5ff",
                            border: "1px solid rgba(0,197,255,0.4)",
                            padding: "0.25rem 0.65rem", borderRadius: "2px",
                        }}
                    >
                        Western Digital
                    </span>
                </div>

                {/* Centre copy */}
                <div style={{ position: "relative", zIndex: 2 }}>
                    <h1
                        className="fw-bold mb-2"
                        style={{
                            fontSize: "3.8rem", color: "#ffffff",
                            letterSpacing: "-0.02em", lineHeight: 1.1
                        }}
                    >
                        FGI STACKER
                    </h1>
                    <div
                        style={{
                            width: "48px", height: "3px",
                            background: "#00c5ff", borderRadius: "2px",
                            marginBottom: "1.25rem"
                        }}
                    />
                    <p style={{
                        fontSize: "0.85rem", color: "rgba(255,255,255,0.65)",
                        letterSpacing: "0.04em", lineHeight: 1.7, margin: 0
                    }}>
                        {/*Desciption line 1*/}<br />
                        {/*Description Line 2*/}
                    </p>
                </div>

                {/* Bottom version tag */}
                <p style={{
                    fontSize: "0.65rem", color: "rgba(255,255,255,0.3)",
                    letterSpacing: "0.1em", margin: 0,
                    position: "relative", zIndex: 2
                }}>
                    v1.0 · PWD STACKER
                </p>
            </div>

            {/* ── Right form panel ── */}
            <div
                className="flex-grow-1 d-flex align-items-center justify-content-center p-4"
                style={{ background: "#f4f5f7" }}
            >
                <div style={{ width: "100%", maxWidth: "380px" }}>

                    {/* Card */}
                    <div
                        className="p-4 p-md-5"
                        style={{
                            background: "#ffffff",
                            borderRadius: "12px",
                            boxShadow: "0 4px 24px rgba(23,43,77,0.10)",
                        }}
                    >
                        {/* Header */}
                        <p
                            className="mb-1 text-uppercase fw-semibold"
                            style={{
                                fontSize: "0.65rem", letterSpacing: "0.25em",
                                color: "#00c5ff"
                            }}
                        >
                            {/*Secure access*/}
                        </p>
                        <h2
                            className="fw-bold mb-4"
                            style={{
                                color: "#172b4d", letterSpacing: "-0.01em",
                                fontSize: "1.65rem"
                            }}
                        >
                            Sign in
                        </h2>

                        <form onSubmit={handleSubmit} noValidate>

                            {/* Username */}
                            <div className="mb-3">
                                <label
                                    htmlFor="username"
                                    className="form-label fw-semibold"
                                    style={{
                                        fontSize: "0.72rem", letterSpacing: "0.08em",
                                        textTransform: "uppercase", color: "#172b4d"
                                    }}
                                >
                                    Username
                                </label>
                                <input
                                    id="username"
                                    type="text"
                                    className="form-control"
                                    autoComplete="username"
                                    placeholder="DOMAIN\username"
                                    value={username}
                                    onChange={(e) => setUsername(e.target.value)}
                                    disabled={loading}
                                    style={{
                                        border: "1.5px solid #dde1e9",
                                        borderRadius: "8px",
                                        color: "#172b4d",
                                        fontSize: "0.9rem",
                                        padding: "0.65rem 0.9rem",
                                        background: "#f4f5f7",
                                        transition: "border-color 0.15s",
                                    }}
                                />
                            </div>

                            {/* Password */}
                            <div className="mb-4">
                                <label
                                    htmlFor="password"
                                    className="form-label fw-semibold"
                                    style={{
                                        fontSize: "0.72rem", letterSpacing: "0.08em",
                                        textTransform: "uppercase", color: "#172b4d"
                                    }}
                                >
                                    Password
                                </label>
                                <input
                                    id="password"
                                    type="password"
                                    className="form-control"
                                    autoComplete="current-password"
                                    placeholder="••••••••"
                                    value={password}
                                    onChange={(e) => setPassword(e.target.value)}
                                    disabled={loading}
                                    style={{
                                        border: "1.5px solid #dde1e9",
                                        borderRadius: "8px",
                                        color: "#172b4d",
                                        fontSize: "0.9rem",
                                        padding: "0.65rem 0.9rem",
                                        background: "#f4f5f7",
                                        transition: "border-color 0.15s",
                                    }}
                                />
                            </div>

                            {/* Error */}
                            {error && (
                                <div
                                    className="mb-3 px-3 py-2"
                                    role="alert"
                                    style={{
                                        background: "rgba(0,82,204,0.06)",
                                        borderLeft: "3px solid #0052cc",
                                        borderRadius: "0 6px 6px 0",
                                        color: "#0052cc",
                                        fontSize: "0.8rem",
                                    }}
                                >
                                    {error}
                                </div>
                            )}

                            {/* Submit */}
                            <button
                                type="submit"
                                className="btn w-100 fw-bold"
                                disabled={loading}
                                style={{
                                    background: loading
                                        ? "#a0b4d6"
                                        : "linear-gradient(90deg, #0052cc 0%, #0065ff 100%)",
                                    color: "#ffffff",
                                    border: "none",
                                    borderRadius: "8px",
                                    padding: "0.72rem",
                                    fontSize: "0.82rem",
                                    letterSpacing: "0.12em",
                                    textTransform: "uppercase",
                                    boxShadow: loading ? "none" : "0 4px 12px rgba(0,82,204,0.3)",
                                    transition: "opacity 0.15s",
                                }}
                            >
                                {loading ? (
                                    <>
                                        <span
                                            className="spinner-border spinner-border-sm me-2"
                                            role="status"
                                            aria-hidden="true"
                                        />
                                        Authenticating…
                                    </>
                                ) : (
                                    "Sign in"
                                )}
                            </button>
                        </form>
                    </div>

                    {/* Below-card hint */}
                    <p
                        className="text-center mt-3 mb-0"
                        style={{
                            fontSize: "0.72rem", color: "#8993a4",
                            letterSpacing: "0.04em"
                        }}
                    >
                        {/*Use your Active Directory credentials.*/}
                    </p>
                </div>
            </div>
        </div>
    );
}