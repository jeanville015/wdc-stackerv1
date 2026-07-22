namespace WDC_STACKER.API.Models.Stacker
{
    public class ShipBoxView
    {
        public bool IsSuggestedTarget { get; set; }
        public string BoxNo { get; set; } = string.Empty;
        public string ShipBoxName { get; set; } = string.Empty;
        public string ShipBoxStatus { get; set; } = string.Empty;
        public int ShipBoxNum { get; set; }
        public int LayerRowNum { get; set; }
        public int LayerColNum { get; set; }
        public int ShipBoxListCount { get; set; }
        public decimal ShipBoxListPercentage { get; set; }
        public bool HasReleaseStatus { get; set; }
    }
}
