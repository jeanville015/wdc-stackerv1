namespace WDC_STACKER.API.Models
{
    // ── Login ────────────────────────────────────────────────────────────────
    public class SoapLoginRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class SoapLoginResponse
    {
        public bool Success { get; set; }
        public string Token { get; set; } = string.Empty;   // session / JWT token
        public string Message { get; set; } = string.Empty;
    }

    public class ADLoginRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class ADLoginResponse
    {
        public bool Success { get; set; }
        public string Token { get; set; } = string.Empty;   // session / JWT token
        public string Message { get; set; } = string.Empty;
    }

    // ── Verification ─────────────────────────────────────────────────────────
    public class SoapVerificationRequest
    {
        public string Token { get; set; } = string.Empty;
    }

    public class SoapVerificationResponse
    {
        public bool IsValid { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    // ── GET (generic SOAP read) ───────────────────────────────────────────────
    //   Replace "Payload" / "Result" with the actual data shape once known.
    public class SoapGetRequest
    {
        public string Token { get; set; } = string.Empty;
        public string ResourceId { get; set; } = string.Empty;  // e.g. item ID, device ID
    }

    public class SoapGetResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public object? Data { get; set; }                     // swap for a typed DTO later
    }

    // ── SET (generic SOAP write) ──────────────────────────────────────────────
    public class SoapSetRequest
    {
        public string Token { get; set; } = string.Empty;
        public string ResourceId { get; set; } = string.Empty;
        public object? Payload { get; set; }                  // swap for a typed DTO later
    }

    public class SoapSetResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
