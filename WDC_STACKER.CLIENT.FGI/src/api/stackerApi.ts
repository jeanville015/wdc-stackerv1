import { API_BASE } from "../config/apiConfig";
import type { ScanResponse, AssignRequest, AssignResponse, BoxAssignment, DisassociateResponse, ShipBoxView } from "../types/stacker";
import { STACKER_CLIENT, STACKER_CLIENT_HEADER } from "../config/processConfig";

const CLIENT_HEADERS: Record<string, string> = {
    [STACKER_CLIENT_HEADER]: STACKER_CLIENT
};

function getErrorMessage(err: unknown, fallback: string): string {
    const error = err as { message?: string; Message?: string };
    return error.message ?? error.Message ?? fallback;
}

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

export async function scanApi(holder: string, token: string): Promise<ScanResponse> {
    const response = await fetch(`${API_BASE}/api/stacker/scan`, {
        method: "POST",
        headers: {
            ...CLIENT_HEADERS,
            "Content-Type": "application/json",
            Authorization: `Bearer ${token}`,
        },
        body: JSON.stringify({ Holder: holder }),
    });

    if (!response.ok) {
        const err = await response.json().catch(() => ({}));
        throw new Error(getErrorMessage(err, "Scan failed."));
    }

    return response.json() as Promise<ScanResponse>;
}

export async function assignApi(request: AssignRequest, token: string): Promise<AssignResponse>{
    const response = await fetch(`${API_BASE}/api/stacker/assign`, {
        method: "POST",
        headers: {
            ...CLIENT_HEADERS,
            "Content-Type": "application/json",
            Authorization: `Bearer ${token}`,
        },
        body: JSON.stringify(request),
    });

    if (!response.ok) {
        const err = await response.json().catch(() => ({}));
        throw new Error(getErrorMessage(err, "Assign failed."));
    }

    return response.json() as Promise<AssignResponse>;
}

export async function getBoxAssignmentsApi(boxName: string, token: string): Promise<BoxAssignment[]> {
    const response = await fetch(
        `${API_BASE}/api/stacker/boxes/${encodeURIComponent(boxName)}/assignments`,
        {
            headers: {
                ...CLIENT_HEADERS,
                Authorization: `Bearer ${token}`,
            },
        }
    );

    if (!response.ok) {
        const err = await response.json().catch(() => ({}));
        throw new Error(getErrorMessage(err, "Unable to load box assignments."));
    }

    return response.json() as Promise<BoxAssignment[]>;
}

export async function getShipBoxesApi(
    boxNo: string,
    token: string,
    suggest = false
): Promise<ShipBoxView[]> {
    const response = await fetch(
        `${API_BASE}/api/stacker/boxes/${encodeURIComponent(boxNo)}/shipboxes?suggest=${suggest}`,
        {
            headers: {
                ...CLIENT_HEADERS,
                Authorization: `Bearer ${token}`,
            },
        }
    );

    if (!response.ok) {
        const err = await response.json().catch(() => ({}));
        throw new Error(getErrorMessage(err, "Unable to load ShipBoxes."));
    }

    return response.json() as Promise<ShipBoxView[]>;
}

export async function getShipBoxAssignmentsApi(
    boxName: string,
    shipBoxName: string,
    token: string
): Promise<BoxAssignment[]> {
    const response = await fetch(
        `${API_BASE}/api/stacker/boxes/${encodeURIComponent(boxName)}/shipboxes/${encodeURIComponent(shipBoxName)}/assignments`,
        {
            headers: {
                ...CLIENT_HEADERS,
                Authorization: `Bearer ${token}`,
            },
        }
    );

    if (!response.ok) {
        const err = await response.json().catch(() => ({}));
        throw new Error(getErrorMessage(err, "Unable to load ShipBox assignments."));
    }

    return response.json() as Promise<BoxAssignment[]>;
}

export async function disassociateHolderApi(holder: string, token: string): Promise<DisassociateResponse> {
    const response = await fetch(`${API_BASE}/api/stacker/assignments`, {
        method: "DELETE",
        headers: {
            ...CLIENT_HEADERS,
            "Content-Type": "application/json",
            Authorization: `Bearer ${token}`,
        },
        body: JSON.stringify({ Holder: holder }),
    });

    if (!response.ok) {
        const err = await response.json().catch(() => ({}));
        throw new Error(getErrorMessage(err, "Unable to disassociate holder."));
    }

    return response.json() as Promise<DisassociateResponse>;
}

export async function disassociateFgiHolder(holder: string, token: string): Promise<DisassociateResponse> {
    const response = await fetch(`${API_BASE}/api/stacker/fgi/hold-assignments`, {
        method: "DELETE",
        headers: {
            ...CLIENT_HEADERS,
            "Content-Type": "application/json",
            Authorization: `Bearer ${token}`,
        },
        body: JSON.stringify({ Holder: holder }),
    });

    if (!response.ok) {
        const err = await response.json().catch(() => ({}));
        throw new Error(getErrorMessage(err, "Unable to disassociate holder."));
    }

    return response.json() as Promise<DisassociateResponse>;
}

export async function getBoxesApi(token: string): Promise<ScanResponse> {
    const response = await fetch(`${API_BASE}/api/stacker/boxes`, {
        method: "GET",
        headers: {
            ...CLIENT_HEADERS,
            Authorization: `Bearer ${token}`,
        },
    });

    if (!response.ok) {
        const err = await response.json().catch(() => ({}));
        throw new Error(getErrorMessage(err, "Unable to load boxes."));
    }

    return response.json() as Promise<ScanResponse>;
}

export async function exportCsvApi(token: string): Promise<void> {
    const response = await fetch(`${API_BASE}/api/stacker/export/csv`, {
        method: "GET",
        headers: {
            ...CLIENT_HEADERS,
            Authorization: `Bearer ${token}`,
        },
    });

    if (!response.ok) {
        const err = await response.json().catch(() => ({}));
        throw new Error(getErrorMessage(err, "Unable to export CSV."));
    }

    const blob = await response.blob();
    const url = window.URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;

    // Get filename from Content-Disposition header, fallback to default
    const contentDisposition = response.headers.get('Content-Disposition');
    let filename = 'rack_export.csv';
    if (contentDisposition) {
        const filenameMatch = contentDisposition.match(/filename[^;=\n]*=((['"]).*?\2|[^;\n]*)/);
        if (filenameMatch && filenameMatch[1]) {
            filename = filenameMatch[1].replace(/['"]/g, '');
        }
    }

    a.download = filename;
    document.body.appendChild(a);
    a.click();
    window.URL.revokeObjectURL(url);
    document.body.removeChild(a);
}
