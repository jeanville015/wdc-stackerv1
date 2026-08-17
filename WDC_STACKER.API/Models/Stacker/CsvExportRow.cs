namespace WDC_STACKER.API.Models.Stacker;

public class CsvExportRow
{
    public string Holder { get; set; } = string.Empty;
    public string Job { get; set; } = string.Empty;
    public int Qty { get; set; }
    public string Grade { get; set; } = string.Empty;
    public string BlackBox { get; set; } = string.Empty;
    public string ShipBox { get; set; } = string.Empty;
    public string InsertedOn { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public string Model { get; set; } = string.Empty;
    public string PartNum { get; set; } = string.Empty;
    public string PenNum { get; set; } = string.Empty;
    public string Lec { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}
