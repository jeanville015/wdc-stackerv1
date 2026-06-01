using System.DirectoryServices.AccountManagement;
using WDC_STACKER.API.Models.Auth;

namespace WDC_STACKER.API.Services
{
    public class ActiveDirectoryService
    {
        private readonly IConfiguration _config;
        private readonly ILogger<ActiveDirectoryService> _logger;
        private readonly string _domain;

        public ActiveDirectoryService(IConfiguration config, ILogger<ActiveDirectoryService> logger)
        {
            _config = config;
            _logger = logger;
            _domain = _config["ActiveDirectory:Domain"] ?? string.Empty;
        }

        public async Task<AdAuthResult> AuthenticateAsync(string username, string password)
        {
            return await Task.Run(() =>
            {
                try
                {
                    var samAccountName = NormalizeUsername(username);

                    using var context = new PrincipalContext(ContextType.Domain, _domain);

                    var isValid = context.ValidateCredentials(samAccountName, password, ContextOptions.Negotiate);

                    if (!isValid)
                    {
                        return new AdAuthResult
                        {
                            IsAuthenticated = false,
                            Username = username,
                            Message = "Invalid username or password."
                        };
                    }

                    var principal = UserPrincipal.FindByIdentity(context, IdentityType.SamAccountName, samAccountName);

                    return new AdAuthResult
                    {
                        IsAuthenticated = true,
                        Username = username,
                        DisplayName = principal?.DisplayName ?? samAccountName,
                        Message = "OK"
                    };
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "AD authentication error for user={Username}", username);

                    return new AdAuthResult
                    {
                        IsAuthenticated = false,
                        Username = username,
                        Message = "Authentication service error."
                    };
                }
            });
        }

        private static string NormalizeUsername(string username)
        {
            if (username.Contains('\\'))
                return username.Split('\\').Last();

            if (username.Contains('@'))
                return username.Split('@').First();

            return username;
        }
    }
}