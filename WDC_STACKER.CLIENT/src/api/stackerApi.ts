import { API_BASE } from "../config/apiConfig";
import type { ScanResponse, AssignResponse } from "../types/stacker";

// test API
export async function testApi(scannedId: string): Promise<ScanResponse> {
    const response = await fetch(`${API_BASE}/api/stacker/scan`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ scannedId }),
    });
    if (!response.ok) {
        const err = await response.json().catch(() => ({}));
        throw new Error((err as { message?: string }).message ?? "Scan failed.");
    }
    return response.json() as Promise<ScanResponse>;
}

export async function scanApi(scannedId: string): Promise<ScanResponse> {
    const response = await fetch(`${API_BASE}/api/stacker/scan`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ scannedId }),
    });
    if (!response.ok) {
        const err = await response.json().catch(() => ({}));
        throw new Error((err as { message?: string }).message ?? "Scan failed.");
    }
    return response.json() as Promise<ScanResponse>;
}

export async function assignApi(): Promise<AssignResponse> {
    const response = await fetch(`${API_BASE}/api/stacker/assign`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
    });
    if (!response.ok) {
        const err = await response.json().catch(() => ({}));
        throw new Error((err as { message?: string }).message ?? "Assign failed.");
    }
    return response.json() as Promise<AssignResponse>;
}