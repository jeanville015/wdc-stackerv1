// ── Stacker operation types ───────────────────────────────────────────────────

/** Response from POST /api/stacker/scan */
export interface ScanResponse {
    success: boolean;
    scannedId: string;
    message: string;
}

/** Response from POST /api/stacker/assign */
export interface AssignResponse {
    success: boolean;
    message: string;
}