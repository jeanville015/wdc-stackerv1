namespace WDC_STACKER.API.Models
{
    public class CapacityConfig
    {
        public int RACK_COUNT { get; set; }
        public int LAYER_COUNT { get; set; }
        public int BOX_COUNT { get; set; }
        public int TARGET_QTY { get; set; }
        public int TARGET_TRAY_COUNT { get; set; }
        public string ValidOperation { get; set; } = string.Empty;
    }
}
