using System.Security.Cryptography;
using WDC_STACKER.API.Models.Auth;
using WDC_STACKER.API.Services;

namespace WDC_STACKER.API.Aggregate
{
    /// <summary>
    /// Aggregation layer for authentication.
    /// Sits between AuthController and ActiveDirectoryService.
    ///
    /// Responsibilities:
    ///   1. Delegate credential verification to ActiveDirectoryService.
    ///   2. On success, generate a session token.
    ///   3. Build and return the LoginResponse ViewModel for the controller.
    ///
    /// Future extensions to add HERE (not in the controller, not in the service):
    ///   - Role / privilege lookup after AD validates the user.
    ///   - Audit log writes to MS SQL on login / logout events.
    ///   - JWT generation (swap the simple token below for a signed JWT).
    ///   - Rate-limiting / lockout policy.
    /// </summary>
    public class AuthProjectionAggregate
    {
        private readonly ActiveDirectoryService _adService;
        private readonly ILogger<AuthProjectionAggregate> _logger;
        private readonly FeatsCredentialStore _featsCredentialStore;
        private readonly StackerAggregate _stackerAggregate;

        public AuthProjectionAggregate(ActiveDirectoryService adService, ILogger<AuthProjectionAggregate> logger, FeatsCredentialStore featsCredentialStore, StackerAggregate stackerAggregate)
        {
            _adService = adService;
            _logger = logger;
            _featsCredentialStore = featsCredentialStore;
            _stackerAggregate = stackerAggregate;
        }

        public async Task<LoginResponse> LoginAsync(LoginRequest request)
        {
            // 1. Delegate to the AD service (bypass or real)
            var adResult = await _adService.AuthenticateAsync(
                request.Username, request.Password);

            if (!adResult.IsAuthenticated)
            {
                _logger.LogWarning(
                    "Login failed for user={Username}: {Message}",
                    request.Username, adResult.Message);

                return new LoginResponse
                {
                    Success = false,
                    Token = string.Empty,
                    Username = request.Username,
                    Message = adResult.Message
                };
            }

            // 2. Generate a session token
            //    TODO: replace with a signed JWT once real AD is wired in.
            var token = GenerateSessionToken();
            _featsCredentialStore.Store(token, request.Username, request.Password);
            _logger.LogInformation("Login successful for user={Username}", adResult.Username);

            var canAccessConfiguration = await _stackerAggregate.CanAccessConfigurationAsync(request.Username, request.Password);

            // 3. Return the ViewModel the controller forwards to React
            return new LoginResponse
            {
                Success = true,
                Token = token,
                Username = adResult.DisplayName,
                CanAccessConfiguration = canAccessConfiguration,
                Message = "Login successful"
            };
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        /// <summary>
        /// Opaque session token for the bypass / dev phase.
        /// Replace with JwtSecurityTokenHandler once real AD is in place.
        /// </summary>
        private static string GenerateSessionToken()
        {
            var bytes = RandomNumberGenerator.GetBytes(32);
            return Convert.ToBase64String(bytes);
        }
    }
}