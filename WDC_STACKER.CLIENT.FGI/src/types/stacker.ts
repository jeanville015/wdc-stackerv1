// ── Stacker operation types ───────────────────────────────────────────────────

export interface ScanResponse {
    Success: boolean;
    CanAssign: boolean;
    Message: string;
    Holder: string;
    CamVersion?: string | null;
    HolderJob: Record<string, string>;
    RawQueryResult: FeatsQueryResponse | null;
    GridViewBoxes: BoxView[];
}

export interface ShipBoxView {
    IsSuggestedTarget?: boolean;
    BoxNo: string;
    ShipBoxName: string;
    Lec: string;
    ShipBoxStatus: string;
    ShipBoxNum: number;
    LayerRowNum: number;
    LayerColNum: number;
    ShipBoxListCount: number;
    ShipBoxListPercentage: number;
    HasHeldHolder?: boolean;
    /** Zero-based indexes (holder-ID ascending order) whose STATUS is HOLD. */
    HeldHolderPositions?: number[];
    HasReleaseStatus: boolean;
    InSiteHoldHolders?: string[];
    /** Zero-based indexes in the holder-ID ascending assignment order. */
    InSiteHoldPositions?: number[];
    HasInSiteHold?: boolean;
}

export interface BoxView {
    BoxNo: string;
    PartNum?: string | null;
    PenNum?: string | null;
    ProductName?: string | null;
    CamVersion?: string | null;
    RackNum: number;
    LayerRowNum: number;
    LayerColNum: number;
    BoxListCount: number;
    BoxListPercentage: number;
    IsSuggestedTarget?: boolean;
    HasReleaseStatus: boolean;
    ShipBoxes?: ShipBoxView[];
}

export interface AssignRequest {
    Holder: string;
    BoxNo: string;
    RackNum: number;
    LayerRowNum: number;
    LayerColNum: number;
    ShipBoxName?: string;
    ShipBoxNum?: number;
    ShipBoxLayerRowNum?: number;
    ShipBoxLayerColNum?: number;
    Process: string;
    CamVersion?: string | null;
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
    Job?: string | null;
    Qty?: number | null;
    ProductName: string;
    Factory: string;
    Lec: string;
    ClassName: string;
    Partnum: string;
    Pennum: string;
    Status: string;
}

export interface DisassociateResponse {
    Success: boolean;
    Message: string;
    GridViewBoxes: BoxView[];
}