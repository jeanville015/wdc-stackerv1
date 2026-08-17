namespace WDC_STACKER.API.Models.Stacker
{
    public class AssignHolderRequest
    {
        public string Holder { get; set; } = string.Empty;
        public string BoxNo { get; set; } = string.Empty;
        public int RackNum { get; set; }
        public int LayerRowNum { get; set; }
        public int LayerColNum { get; set; }
        public string ShipBoxName { get; set; } = string.Empty;
        public int ShipBoxNum { get; set; }
        public int ShipBoxLayerRowNum { get; set; }
        public int ShipBoxLayerColNum { get; set; }
        public string Process { get; set; } = string.Empty;
        public string? CamVersion { get; set; }
    }
}