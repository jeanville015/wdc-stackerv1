export interface CapacityConfig {
    RACK_COUNT: number;
    LAYER_COUNT: number;
    BOX_COUNT: number;
    MAX_ITEM_PER_BOX: number;
    TARGET_QTY: number;
    TARGET_TRAY_COUNT: number;
    ValidOperation: string;
    "LAYER_COUNT-SHIPBOX": number;
    "BOX_COUNT-SHIPBOX": number;
    "MAX_ITEM_PER_BOX-SHIPBOX": number;
    FJ: number;
    FD: number;
    FS: number;
    SJ: string;
    SD: string;
}
