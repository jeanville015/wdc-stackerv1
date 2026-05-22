namespace WDC_STACKER.API.Models.Auth
{
    /// <summary>
    /// Internal result passed from ActiveDirectoryService
    /// to AuthProjectionService. Never exposed directly to the client.
    /// </summary>
    public class AdAuthResult
    {
        public bool IsAuthenticated { get; set; }
        public string Username { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }
}
