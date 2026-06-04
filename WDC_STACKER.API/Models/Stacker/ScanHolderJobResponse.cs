using WDC_STACKER.API.Models.Feats;

namespace WDC_STACKER.API.Models.Stacker
{
    public class ScanHolderJobResponse
    {
        public bool Success { get; set; }
        public bool CanAssign { get; set; }
        public string Message { get; set; } = string.Empty;
        public string Holder { get; set; } = string.Empty;
        public Dictionary<string, string> HolderJob { get; set; } = new();
        public List<BoxView> GridViewBoxes { get; set; } = new();
        public FeatsQueryResponse? RawQueryResult { get; set; }
    }
}
