using WDC_STACKER.API.Models.Feats;

namespace WDC_STACKER.API.Services
{
    public class UserPrivilegesService
    {
        private readonly FeatsService _feats;
        private readonly ILogger<UserPrivilegesService> _logger;

        public UserPrivilegesService(FeatsService feats, ILogger<UserPrivilegesService> logger)
        {
            _feats = feats;
            _logger = logger;
        }

        public async Task<UserPrivilegesResponse> GetAsync(UserPrivilegesRequest request)
        {
            return await _feats.GetUserPrivilegesAsync(
                request.EmployeeName,
                request.FeatsUsername,
                request.FeatsPassword);
        }
    }
}