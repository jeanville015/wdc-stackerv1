using WDC_STACKER.API.Models;

namespace WDC_STACKER.API.Services
{
    /// <summary>
    /// Wraps all outbound SOAP calls.
    /// Replace the placeholder implementations with real HttpClient / SOAP proxy calls.
    /// </summary>
    public class SoapApiService
    {
        private readonly IConfiguration _config;
        private readonly ILogger<SoapApiService> _logger;
        // private readonly HttpClient _http;   // inject when real SOAP calls are wired in

        // Read the SOAP endpoint URL from appsettings.json  →  "SoapApi:BaseUrl"
        private readonly string _baseUrl;

        public SoapApiService(IConfiguration config, ILogger<SoapApiService> logger)
        {
            _config = config;
            _logger = logger;
            _baseUrl = _config["SoapApi:BaseUrl"] ?? "http://localhost/soap";
        }

        // ── LOGIN ─────────────────────────────────────────────────────────────
        public async Task<SoapLoginResponse> LoginAsync(SoapLoginRequest request)
        {
            _logger.LogInformation("SOAP LOGIN → {BaseUrl}", _baseUrl);

            // TODO: build SOAP envelope, call _http.PostAsync, parse response
            // Placeholder — replace with real implementation:
            await Task.Delay(0);

            if (request.Username == "admin" && request.Password == "password")
            {
                return new SoapLoginResponse
                {
                    Success = true,
                    Token = Guid.NewGuid().ToString(),
                    Message = "Login successful"
                };
            }

            return new SoapLoginResponse
            {
                Success = false,
                Token = string.Empty,
                Message = "Invalid credentials"
            };
        }

        // ── VERIFICATION ──────────────────────────────────────────────────────
        public async Task<SoapVerificationResponse> VerifyAsync(SoapVerificationRequest request)
        {
            _logger.LogInformation("SOAP VERIFY token={Token}", request.Token);

            // TODO: validate token against SOAP service or local store
            await Task.Delay(0);

            bool isValid = !string.IsNullOrWhiteSpace(request.Token);

            return new SoapVerificationResponse
            {
                IsValid = isValid,
                Message = isValid ? "Token is valid" : "Token is invalid or expired"
            };
        }

        // ── GET ───────────────────────────────────────────────────────────────
        public async Task<SoapGetResponse> GetAsync(SoapGetRequest request)
        {
            _logger.LogInformation("SOAP GET resource={ResourceId}", request.ResourceId);

            // TODO: build SOAP GetRequest envelope, call remote endpoint, deserialise
            await Task.Delay(0);

            return new SoapGetResponse
            {
                Success = true,
                Message = "GET placeholder",
                Data = new { ResourceId = request.ResourceId, Value = "example-value" }
            };
        }

        // ── SET ───────────────────────────────────────────────────────────────
        public async Task<SoapSetResponse> SetAsync(SoapSetRequest request)
        {
            _logger.LogInformation("SOAP SET resource={ResourceId}", request.ResourceId);

            // TODO: build SOAP SetRequest envelope, call remote endpoint, check ACK
            await Task.Delay(0);

            return new SoapSetResponse
            {
                Success = true,
                Message = "SET placeholder — resource updated"
            };
        }

    }
}
