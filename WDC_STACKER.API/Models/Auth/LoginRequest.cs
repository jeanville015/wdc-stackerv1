namespace WDC_STACKER.API.Models.Auth
{
    /// <summary>
    /// Credentials submitted by the user on the login form.
    /// Received by POST /api/auth/login.
    /// </summary>
    public class LoginRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}
