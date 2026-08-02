import { API_BASE } from "../config/apiConfig";
import {
    STACKER_CLIENT,
    STACKER_CLIENT_HEADER,
} from "../config/processConfig";
import type {
    FgiWithdrawalRack,
    FgiWithdrawalRequest,
    AcknowledgeFgiWithdrawalResponse,
    FgiWithdrawalDisassociationPreview,
    FgiWithdrawalDisassociationRequest,
    FgiWithdrawalDisassociationResponse,
} from "../types/withdrawal";
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

export async function getFgiWithdrawalRequestsApi(
    token: string
): Promise<FgiWithdrawalRequest[]> {
    const response = await fetch(
        `${API_BASE}/api/stacker/withdrawal/requests`,
        { headers: createHeaders(token) }
    );

    if (!response.ok) {
        throw new Error(
            await readError(response, "Unable to load withdrawal requests.")
        );
    }

    return response.json() as Promise<FgiWithdrawalRequest[]>;
}

export async function acknowledgeFgiWithdrawalRequestApi(
    requestId: number,
    token: string
): Promise<AcknowledgeFgiWithdrawalResponse> {
    const response = await fetch(
        `${API_BASE}/api/stacker/withdrawal/requests/${requestId}/acknowledge`,
        {
            method: "PATCH",
            headers: createHeaders(token),
        }
    );

    if (!response.ok) {
        throw new Error(
            await readError(
                response,
                "Unable to acknowledge the withdrawal request."
            )
        );
    }

    return response.json() as Promise<AcknowledgeFgiWithdrawalResponse>;
}

export async function getFgiWithdrawalLayoutApi(
    lec: string,
    token: string
): Promise<FgiWithdrawalRack | null> {
    const response = await fetch(
        `${API_BASE}/api/stacker/withdrawal/layout?lec=${encodeURIComponent(lec)}`,
        { headers: createHeaders(token) }
    );

    if (!response.ok) {
        throw new Error(
            await readError(response, "Unable to load withdrawal layout.")
        );
    }

    if (response.status === 204) {
        return null;
    }

    return response.json() as Promise<FgiWithdrawalRack | null>;
}

export async function getFgiWithdrawalDisassociationPreviewApi(
    lec: string,
    penNum: string,
    total: number,
    token: string
): Promise<FgiWithdrawalDisassociationPreview> {
    const query = new URLSearchParams({
        lec,
        total: String(total),
    });

    if (penNum.trim()) {
        query.set("penNum", penNum.trim());
    }

    const response = await fetch(
        `${API_BASE}/api/stacker/withdrawal/disassociation-preview?${query.toString()}`,
        { headers: createHeaders(token) }
    );

    if (!response.ok) {
        throw new Error(
            await readError(
                response,
                "Unable to load the disassociation preview."
            )
        );
    }

    return response.json() as
        Promise<FgiWithdrawalDisassociationPreview>;
}

export async function
    disassociateFgiWithdrawalRequestApi(
        requestId: number,
        shippingId: string,
        includedHolders: string[],
        token: string
    ): Promise<FgiWithdrawalDisassociationResponse> {
    const request:
        FgiWithdrawalDisassociationRequest = {
        ShippingId: shippingId,
        IncludedHolders: includedHolders,
    };

    const response = await fetch(
        `${API_BASE}/api/stacker/withdrawal/requests/${requestId}/disassociate`,
        {
            method: "POST",
            headers: {
                ...createHeaders(token),
                "Content-Type": "application/json",
            },
            body: JSON.stringify(request),
        }
    );

    if (!response.ok) {
        throw new Error(
            await readError(
                response,
                "Unable to disassociate the included Holders."
            )
        );
    }

    return response.json() as
        Promise<FgiWithdrawalDisassociationResponse>;
}
