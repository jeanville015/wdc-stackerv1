namespace WDC_STACKER.API.Models.Auth
{
    /// <summary>
    /// Result returned to the React client after a login attempt.
    /// Sent by POST /api/auth/login.
    /// </summary>
    public class LoginResponse
    {
        public bool Success { get; set; }
        public string Token { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public bool CanAccessConfiguration { get; set; }
        public DateTime ExpiresAt { get; set; }
    }
}