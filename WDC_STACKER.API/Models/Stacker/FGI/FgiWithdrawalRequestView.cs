namespace WDC_STACKER.API.Models.Stacker;

public sealed class FgiWithdrawalRequestView
{
    public long RequestId { get; set; }
    public DateTime? Date { get; set; }
    public string Requestor { get; set; } = string.Empty;
    public string Shift { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Grade { get; set; } = string.Empty;
    public string SliderPartNumber { get; set; } = string.Empty;
    public string HeadType { get; set; } = string.Empty;
    public int? Total { get; set; }
    public string Remarks { get; set; } = string.Empty;
    public string AcknowledgeBy { get; set; } = string.Empty;
    public int? ActualOutput { get; set; }
    public string Status { get; set; } = string.Empty;

    // Returned to the client but not displayed in the table.
    public string Lec { get; set; } = string.Empty;
    public string PenNum { get; set; } = string.Empty;
}

public sealed class FgiWithdrawalDisassociationPreviewView
{
    public int Total { get; set; }
    public long TotalQty { get; set; }
    public int Tolerance { get; set; }
    public long MaximumTotalQty { get; set; }
    public List<FgiWithdrawalSourceRecordView> SourceRecords { get; set; } = [];
}

public sealed class FgiWithdrawalDisassociationRequest
{
    public string ShippingId { get; set; } = string.Empty;
    public List<string> IncludedHolders { get; set; } = [];
}

public sealed class FgiWithdrawalDisassociationResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int DeletedHolderCount { get; set; }
    public int DeletedShipBoxCount { get; set; }
    public int DeletedBoxCount { get; set; }
}

public sealed class FgiWithdrawalSourceRecordView
{
    public string Holder { get; set; } = string.Empty;
    public long Qty { get; set; }
    public DateTime? UpdateTs { get; set; }
    public long RunningTotal { get; set; }
    public bool IsIncluded { get; set; }
    public string Status { get; set; } = string.Empty;
}

public sealed class FgiWithdrawalRackView
{
    public int RackNum { get; set; }
    public List<FgiWithdrawalBoxView> Boxes { get; set; } = [];
}

public sealed class FgiWithdrawalBoxView
{
    public string BoxNo { get; set; } = string.Empty;
    public int LayerRowNum { get; set; }
    public int LayerColNum { get; set; }
    public List<FgiWithdrawalShipBoxView> ShipBoxes { get; set; } = [];
}

public sealed class FgiWithdrawalShipBoxView
{
    public string ShipBoxName { get; set; } = string.Empty;
    public int ShipBoxNum { get; set; }
    public int LayerRowNum { get; set; }
    public int LayerColNum { get; set; }
    public List<FgiWithdrawalHolderView> Holders { get; set; } = [];
}

public sealed class FgiWithdrawalHolderView
{
    public string Holder { get; set; } = string.Empty;
    public int Qty { get; set; }
}
