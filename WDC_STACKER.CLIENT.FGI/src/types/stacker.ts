// ── Stacker operation types ───────────────────────────────────────────────────

/** Response from POST /api/stacker/scan */
export interface ScanResponse {
    Success: boolean;
    CanAssign: boolean;
    Message: string;
    Holder: string;
    HolderJob: Record<string, string>; 
    RawQueryResult: FeatsQueryResponse | null;
    GridViewBoxes: BoxView[];

}
export interface BoxView {
    BoxNo: string;
    RackNum: number;
    LayerRowNum: number;
    LayerColNum: number;
    BoxListCount: number;
    BoxListPercentage: number;
    IsSuggestedTarget?: boolean;
    HasReleaseStatus: boolean;
}
/** ---------------------------------- */


/** Response from POST /api/stacker/assign */
export interface AssignRequest {
    Holder: string;
    BoxNo: string;
    RackNum: number;
    LayerRowNum: number;
    LayerColNum: number;
    Process: string;
}
export interface AssignResponse {
    Success: boolean;
    Message: string;
    Holder: string;
    BoxName: string;
    Lec: string;
    BoxDetailsCreated: boolean;
    GridViewBoxes: BoxView[];
}

export interface FeatsQueryTableResult {
    RootName: string;
    Columns: string[];
    Rows: Array<Record<string, string>>;
}

export interface FeatsQueryResponse {
    Success: boolean;
    Message: string;
    QueryType: string;
    HasMoreRows: boolean;
    RawXml: string;
    ParsedResult: FeatsQueryTableResult;
}

export interface BoxAssignment {
    Holder: string;
    ProductName: string;
    Factory: string;
    Lec: string;
    Status: string;
}

export interface DisassociateResponse {
    Success: boolean;
    Message: string;
    GridViewBoxes: BoxView[];
}