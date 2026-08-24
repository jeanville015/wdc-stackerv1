export interface FgiUnshipChildHolder {
    Holder: string;
    PartNumber: string;
    Grade: string;
    Model: string;
    Qty: number;
    // Position?: number; // TODO: enable once the FEATS HolderJob field name for Position is confirmed.
}

export interface FgiUnshipScanResult {
    Success: boolean;
    Message: string;
    ShippingId: string;
    CamVersion: string | null;
    ChildHolders: FgiUnshipChildHolder[];
}

export interface FgiUnshipResult {
    Success: boolean;
    Message: string;
    ShippingId: string;
    ProcessedHolderCount: number;
}
