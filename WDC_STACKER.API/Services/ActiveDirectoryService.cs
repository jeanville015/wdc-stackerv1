using WDC_STACKER.API.Models.Auth;

namespace WDC_STACKER.API.Services
{
    /// <summary>
    /// Authenticates a user against Active Directory.
    ///
    /// CURRENT MODE: hardcoded test credential (admin / admin).
    ///   Any other username or password returns a failed result.
    ///
    /// TO ENABLE REAL AD:
    ///   1. Add NuGet package:  System.DirectoryServices.AccountManagement
    ///   2. Uncomment the real implementation block below.
    ///   3. Set "ActiveDirectory:Domain" in appsettings.json.
    ///   4. Remove the bypass block entirely.
    /// </summary>
    public class ActiveDirectoryService
    {
        private readonly IConfiguration _config;
        private readonly ILogger<ActiveDirectoryService> _logger;

        // appsettings.json  →  "ActiveDirectory:Domain"
        //   e.g.  "legacy.shared"  or  "MYDOMAIN"
        private readonly string _domain;

        public ActiveDirectoryService(IConfiguration config,
                                      ILogger<ActiveDirectoryService> logger)
        {
            _config = config;
            _logger = logger;
            _domain = _config["ActiveDirectory:Domain"] ?? "legacy.shared";
        }

        public async Task<AdAuthResult> AuthenticateAsync(string username, string password)
        {
            _logger.LogInformation("AD auth attempt for user={Username}", username);

            // ── HARDCODED TEST CREDENTIAL (active) ───────────────────────────
            // Only  admin / admin  is accepted during development.
            // Remove this block and uncomment real AD below when ready.
            await Task.CompletedTask;   // keeps method signature async

            const string TestUser = "admin";
            const string TestPass = "admin";

            if (username == TestUser && password == TestPass)
            {
                _logger.LogWarning(
                    "Hardcoded credential accepted for user={Username}. Replace with real AD.",
                    username);

                return new AdAuthResult
                {
                    IsAuthenticated = true,
                    Username = username,
                    DisplayName = "Administrator",
                    Message = "Test credential accepted."
                };
            }

            _logger.LogWarning(
                "Login rejected — user={Username} did not match test credential.", username);

            return new AdAuthResult
            {
                IsAuthenticated = false,
                Username = username,
                DisplayName = string.Empty,
                Message = "Invalid username or password."
            };
            // ── END HARDCODED CREDENTIAL ──────────────────────────────────────


            /* ── REAL AD IMPLEMENTATION (disabled) ─────────────────────────────
             * Uncomment when System.DirectoryServices.AccountManagement is added.
             *
             * using System.DirectoryServices.AccountManagement;
             *
             * try
             * {
             *     await Task.Run(() =>
             *     {
             *         using var context = new PrincipalContext(
             *             ContextType.Domain, _domain);
             *
             *         bool valid = context.ValidateCredentials(username, password);
             *         if (!valid)
             *         {
             *             return new AdAuthResult
             *             {
             *                 IsAuthenticated = false,
             *                 Message         = "Invalid username or password."
             *             };
             *         }
             *
             *         var principal = UserPrincipal.FindByIdentity(
             *             context, IdentityType.SamAccountName, username);
             *
             *         return new AdAuthResult
             *         {
             *             IsAuthenticated = true,
             *             Username        = username,
             *             DisplayName     = principal?.DisplayName ?? username,
             *             Message         = "OK"
             *         };
             *     });
             * }
             * catch (Exception ex)
             * {
             *     _logger.LogError(ex, "AD authentication error for user={Username}", username);
             *     return new AdAuthResult
             *     {
             *         IsAuthenticated = false,
             *         Message         = "Authentication service error."
             *     };
             * }
             * ── END REAL AD ─────────────────────────────────────────────────*/
        }
    }
}