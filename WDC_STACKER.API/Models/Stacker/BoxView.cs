

namespace WDC_STACKER.API.Models.Stacker
{
    public class BoxView
    {
        public bool IsSuggestedTarget { get; set; }
        public string BoxNo { get; set; } = string.Empty;
        public int RackNum { get; set; }
        public int LayerRowNum { get; set; }
        public int LayerColNum { get; set; }
        public int BoxListCount { get; set; }
        public decimal BoxListPercentage { get; set; }
        public bool HasReleaseStatus { get; set; }
        public List<ShipBoxView> ShipBoxes { get; set; } = new();
    } 
}
