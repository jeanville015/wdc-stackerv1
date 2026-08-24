namespace WDC_STACKER.API.Models.Stacker;

/// <summary>
/// A single child holder loaded from FEATS Query(HolderJob) filtered by
/// ParentHolder = ShippingId, as part of the Job Unship flow (step 1).
/// </summary>
public sealed class FgiUnshipChildHolderView
{
    public string Holder { get; set; } = string.Empty;
    public string PartNumber { get; set; } = string.Empty;
    public string Grade { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public int Qty { get; set; }

    // TODO: The exact FEATS HolderJob field name for "Position" has not been
    // confirmed yet. Uncomment and wire up once verified.
    // public int Position { get; set; }
}

/// <summary>
/// Result of scanning a Shipping Id for the Job Unship flow (step 1):
/// the list of child holders that must be scanned/validated (step 2)
/// before the Unship button can be enabled.
/// </summary>
public sealed class FgiUnshipScanResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string ShippingId { get; set; } = string.Empty;
    public string? CamVersion { get; set; }
    public List<FgiUnshipChildHolderView> ChildHolders { get; set; } = [];
}

/// <summary>
/// Result of executing the Job Unship FEATS transaction sequence
/// (steps 3-6) for a given Shipping Id.
/// </summary>
public sealed class FgiUnshipResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string ShippingId { get; set; } = string.Empty;
    public int ProcessedHolderCount { get; set; }
}
