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
    }

    public class HolderAssignInsertData
    {
        public string Holder { get; set; } = string.Empty;
        public string BoxName { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string Lec { get; set; } = string.Empty;
        public string Factory { get; set; } = string.Empty;
        public string Process { get; set; } = string.Empty;
        public string UpdateBy { get; set; } = string.Empty;
        public DateTime UpdateTs { get; set; }
    }
}