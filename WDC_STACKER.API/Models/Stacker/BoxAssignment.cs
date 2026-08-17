namespace WDC_STACKER.API.Models.Stacker;

public class BoxAssignment
{
    public string Holder { get; set; } = string.Empty;
    public string? Job { get; set; }
    public int? Qty { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string Factory { get; set; } = string.Empty;
    public string Lec { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Partnum { get; set; } = string.Empty;
    public string Pennum { get; set; } = string.Empty;
}

public class DisassociateHolderRequest
{
    public string Holder { get; set; } = string.Empty;
}