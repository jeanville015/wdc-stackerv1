import { API_BASE } from "../config/apiConfig";
import {
    STACKER_CLIENT,
    STACKER_CLIENT_HEADER,
} from "../config/processConfig";
import type {
    FgiUnshipResult,
    FgiUnshipScanResult,
} from "../types/unship";

const createHeaders = (token: string): HeadersInit => ({
    [STACKER_CLIENT_HEADER]: STACKER_CLIENT,
    Authorization: `Bearer ${token}`,
});

async function readError(response: Response, fallback: string): Promise<string> {
    const body = await response.json().catch(() => ({})) as {
        message?: string;
        Message?: string;
    };

    return body.message ?? body.Message ?? fallback;
}

export async function scanUnshipShippingIdApi(
    shippingId: string,
    token: string
): Promise<FgiUnshipScanResult> {
    const response = await fetch(
        `${API_BASE}/api/stacker/unship/scan?shippingId=${encodeURIComponent(shippingId)}`,
        {
            method: "GET",
            headers: createHeaders(token),
        }
    );

    if (!response.ok) {
        throw new Error(
            await readError(
                response,
                "Unable to load the child holders for this Shipping Id."
            )
        );
    }

    return response.json() as Promise<FgiUnshipScanResult>;
}

export async function unshipFgiJobApi(
    shippingId: string,
    token: string
): Promise<FgiUnshipResult> {
    const response = await fetch(
        `${API_BASE}/api/stacker/unship/${encodeURIComponent(shippingId)}`,
        {
            method: "POST",
            headers: createHeaders(token),
        }
    );

    if (!response.ok) {
        throw new Error(
            await readError(
                response,
                "Unable to unship the selected Shipping Id."
            )
        );
    }

    return response.json() as Promise<FgiUnshipResult>;
}
