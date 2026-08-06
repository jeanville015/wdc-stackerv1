namespace WDC_STACKER.API.Models.Stacker
{
    public class ShipBoxView
    {
        public bool IsSuggestedTarget { get; set; }
        public string BoxNo { get; set; } = string.Empty;
        public string ShipBoxName { get; set; } = string.Empty;
        public string Lec { get; set; } = string.Empty;
        public string ShipBoxStatus { get; set; } = string.Empty;
        public int ShipBoxNum { get; set; }
        public int LayerRowNum { get; set; }
        public int LayerColNum { get; set; }
        public int ShipBoxListCount { get; set; }
        public decimal ShipBoxListPercentage { get; set; }
        public bool HasReleaseStatus { get; set; }
        public bool HasHeldHolder { get; set; }

        // ── In-site hold (AHS-backed) display fields ────────────────────────────
        // Populated by StackerAggregate.PopulateFgiInSiteHoldStatusAsync when a
        // valid session token is available. Kept alongside HasHeldHolder (SQL
        // STATUS-based) rather than replacing it, so existing callers are unaffected.
        public List<string> InSiteHoldHolders { get; set; } = new();

        /// <summary>Zero-based indexes in the holder-ID ascending assignment order.</summary>
        public List<int> InSiteHoldPositions { get; set; } = new();
        public bool HasInSiteHold => InSiteHoldPositions.Count > 0;
    }
}
