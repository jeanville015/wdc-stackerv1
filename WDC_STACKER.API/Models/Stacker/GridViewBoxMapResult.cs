namespace WDC_STACKER.API.Models.Stacker
{
    public class GridViewBoxMapResult
    {
        public List<BoxView> Boxes { get; set; } = new();
        public bool HasSuggestedTarget { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}