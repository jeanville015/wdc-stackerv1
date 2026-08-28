

using System.Text.Json.Serialization;

namespace WDC_STACKER.API.Models.Stacker
{
    public class BoxView
    {
        public bool IsSuggestedTarget { get; set; }
        public string BoxNo { get; set; } = string.Empty;
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? PartNum { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? PenNum { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? ProductName { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? CamVersion { get; set; }
        public int RackNum { get; set; }
        public int LayerRowNum { get; set; }
        public int LayerColNum { get; set; }
        public int BoxListCount { get; set; }
        public decimal BoxListPercentage { get; set; }
        public bool HasReleaseStatus { get; set; }
        /// <summary>
        /// Zero-based indexes in the same UPDATETS/HOLDER order used by
        /// GetBoxAssignmentsAsync.
        /// </summary>
        public List<int> ReleaseHolderPositions { get; set; } = new();

        /// <summary>
        /// Zero-based indexes in the same UPDATETS/HOLDER order used by
        /// GetBoxAssignmentsAsync.
        /// </summary>
        public List<int> HeldHolderPositions { get; set; } = new();
        public List<ShipBoxView> ShipBoxes { get; set; } = new();
    } 
}
