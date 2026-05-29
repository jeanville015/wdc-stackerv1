namespace WDC_STACKER.API.Models.Feats
{
    public class FeatsQueryRequest
    {
        public string QueryType { get; set; } = string.Empty;
        public List<string> FieldNames { get; set; } = new();
        public List<FeatsQueryFilter> Filters { get; set; } = new();
        public int RecordLimit { get; set; } = 100;
    }

    public class FeatsQueryFilter
    {
        public string FilterName { get; set; } = string.Empty;
        public string FilterValue { get; set; } = string.Empty;
    }

    public class FeatsQueryResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string QueryType { get; set; } = string.Empty;
        public bool HasMoreRows { get; set; }
        public string RawXml { get; set; } = string.Empty;
        public FeatsQueryTableResult ParsedResult { get; set; } = new();
    }

    public class FeatsQueryTableResult
    {
        public string RootName { get; set; } = string.Empty;
        public List<string> Columns { get; set; } = new();
        public List<Dictionary<string, string>> Rows { get; set; } = new();
    }
}
