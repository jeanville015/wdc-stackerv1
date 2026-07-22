using System.Text.Json.Serialization;
namespace WDC_STACKER.API.Models
{
    public class CapacityConfig
    {
        public int RACK_COUNT { get; set; }
        public int LAYER_COUNT { get; set; }
        public int BOX_COUNT { get; set; }
        public int MAX_ITEM_PER_BOX { get; set; }
        [JsonPropertyName("LAYER_COUNT-SHIPBOX")]
        public int LAYER_COUNT_SHIPBOX { get; set; }
        [JsonPropertyName("BOX_COUNT-SHIPBOX")]
        public int BOX_COUNT_SHIPBOX { get; set; } 
        [JsonPropertyName("MAX_ITEM_PER_BOX-SHIPBOX")]
        public int MAX_ITEM_PER_BOX_SHIPBOX { get; set; }
        public int TARGET_QTY { get; set; }
        public int TARGET_TRAY_COUNT { get; set; }
        public string ValidOperation { get; set; } = string.Empty;
        public int FJ { get; set; }
        public int FD { get; set; }
        public int FS { get; set; }
        public string SJ { get; set; } = string.Empty;
        public string SD { get; set; } = string.Empty; 
    }
}
