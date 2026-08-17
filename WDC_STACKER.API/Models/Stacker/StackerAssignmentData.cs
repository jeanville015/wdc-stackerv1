namespace WDC_STACKER.API.Models.Stacker
{
    public class BoxDetailsInsertData
    {
        public string BoxNo { get; set; } = string.Empty;
        public int RackNum { get; set; }
        public int LayerRowNum { get; set; }
        public int LayerColNum { get; set; }
        public string UpdateBy { get; set; } = string.Empty;
        public DateTime UpdateTs { get; set; }
        public string ClientCode { get; set; } = string.Empty;
    }

    public class ShipBoxDetailsInsertData
    {
        public string BoxNo { get; set; } = string.Empty;
        public string ShipBoxName { get; set; } = string.Empty;
        public string ShipBoxStatus { get; set; } = string.Empty;
        public int ShipBoxNum { get; set; }
        public int LayerRowNum { get; set; }
        public int LayerColNum { get; set; }
        public string UpdateBy { get; set; } = string.Empty;
        public DateTime UpdateTs { get; set; } 
    }

    public class HolderAssignInsertData
    {
        public string Holder { get; set; } = string.Empty;
        public string BoxName { get; set; } = string.Empty;
        public string PartNum { get; set; } = string.Empty;
        public string? PenNum { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string? Lec { get; set; }
        public int? Qty { get; set; }
        public string Factory { get; set; } = string.Empty;
        public string ShipBoxName { get; set; } = string.Empty;
        public string Process { get; set; } = string.Empty;
        public string BinName { get; set; } = string.Empty;
        public string? CamVersion { get; set; }
        public string? Job { get; set; }
        public string? Status { get; set; }
        public string UpdateBy { get; set; } = string.Empty;
        public DateTime UpdateTs { get; set; }
    }
}