namespace WDC_STACKER.API.Models.Stacker
{
    public class AssignHolderRequest
    {
        public string Holder { get; set; } = string.Empty;
        public string BoxNo { get; set; } = string.Empty;
        public int RackNum { get; set; }
        public int LayerRowNum { get; set; }
        public int LayerColNum { get; set; }
        public string Process { get; set; } = string.Empty;
    }
}