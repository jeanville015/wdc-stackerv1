export interface FgiWithdrawalRequest {
    RequestId: number;
    Date: string | null;
    Requestor: string;
    Shift: string;
    Model: string;
    Category: string;
    Grade: string;
    SliderPartNumber: string;
    HeadType: string;
    Total: number | null;
    Remarks: string;
    AcknowledgeBy: string;
    ActualOutput: number | null;
    Status: string;
    Lec: string;
    PenNum: string;
}

export interface FgiWithdrawalHolder {
    Holder: string;
    Qty: number;
    ProductName: string;
    Factory: string;
    Status: string;
    IsInSiteHold?: boolean;
}

export interface FgiWithdrawalShipBox {
    ShipBoxName: string;
    ShipBoxNum: number;
    LayerRowNum: number;
    LayerColNum: number;
    Holders: FgiWithdrawalHolder[];
    Lec: string;
}

export interface FgiWithdrawalBox {
    BoxNo: string;
    LayerRowNum: number;
    LayerColNum: number;
    ShipBoxes: FgiWithdrawalShipBox[];
    Grade: string;
    PartNum: string;
    PenNum: string;
}

export interface FgiWithdrawalRack {
    RackNum: number;
    Boxes: FgiWithdrawalBox[];
}

export interface AcknowledgeFgiWithdrawalResponse {
    Success: boolean;
    Message: string;
    AcknowledgeBy: string;
}

export interface FgiWithdrawalSourceRecord {
    Holder: string;
    Qty: number;
    UpdateTs: string | null;
    RunningTotal: number;
    IsIncluded: boolean;
    Status: string;
    WasReviewedForHold: boolean;
}

export interface FgiWithdrawalDisassociationPreview {
    Total: number;
    TotalQty: number;
    Tolerance: number;
    MaximumTotalQty: number;
    SourceRecords: FgiWithdrawalSourceRecord[];
}

export interface FgiWithdrawalDisassociationRequest {
    IncludedHolders: string[];
    ShippingId: string;
}

export interface FgiWithdrawalDisassociationResponse {
    Success: boolean;
    Message: string;
    DeletedHolderCount: number;
    DeletedShipBoxCount: number;
    DeletedBoxCount: number;
}