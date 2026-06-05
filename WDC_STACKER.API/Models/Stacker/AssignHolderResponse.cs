using WDC_STACKER.API.Models.Feats;

namespace WDC_STACKER.API.Models.Stacker
{
    public class AssignHolderResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string Holder { get; set; } = string.Empty;
        public string BoxName { get; set; } = string.Empty;
        public string Lec { get; set; } = string.Empty;
        public bool BoxDetailsCreated { get; set; }
        public FeatsQueryResponse? RawQueryResult { get; set; }
        public List<BoxView> GridViewBoxes { get; set; } = new();
    }
}