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
}

export interface FgiWithdrawalShipBox {
    ShipBoxName: string;
    ShipBoxNum: number;
    LayerRowNum: number;
    LayerColNum: number;
    Holders: FgiWithdrawalHolder[];
}

export interface FgiWithdrawalBox {
    BoxNo: string;
    LayerRowNum: number;
    LayerColNum: number;
    ShipBoxes: FgiWithdrawalShipBox[];
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
}

export interface FgiWithdrawalDisassociationPreview {
    Total: number;
    TotalQty: number;
    Tolerance: number;
    MaximumTotalQty: number;
    SourceRecords: FgiWithdrawalSourceRecord[];
}

export interface FgiWithdrawalDisassociationRequest {
    ShippingId: string;
    IncludedHolders: string[];
}

export interface FgiWithdrawalDisassociationResponse {
    Success: boolean;
    Message: string;
    DeletedHolderCount: number;
    DeletedShipBoxCount: number;
    DeletedBoxCount: number;
}