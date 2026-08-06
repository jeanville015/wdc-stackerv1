namespace WDC_STACKER.API.Models.Stacker;

public class KittingRequest
{
    public int ID { get; set; }
    public string GRADE { get; set; } = string.Empty;
    public string SLIDERPARTNUMBER { get; set; } = string.Empty;
    public int TOTAL { get; set; }
    public string? LEC { get; set; }
    public string? PENNUM { get; set; }
    public string? ACKNOWLEDGEBY { get; set; }
    public int? ACTUALOUTPUT { get; set; }
}

public class AcknowledgeKittingRequest
{
    public int ID { get; set; }
    public string AcknowledgedBy { get; set; } = string.Empty;
}

public class AcknowledgeKittingResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
}
