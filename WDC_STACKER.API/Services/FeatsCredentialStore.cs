namespace WDC_STACKER.API.Services
{
    public class FeatsCredentialStore
    {
        private readonly Dictionary<string, FeatsCredentials> _credentialsByToken = new();

        public void Store(string token, string username, string password, DateTime expiresAt)
        {
            _credentialsByToken[token] = new FeatsCredentials
            {
                Username = username,
                Password = password,
                ExpiresAt = expiresAt
            };
        }

        public bool TryGet(string token, out FeatsCredentials credentials)
        {
            if (_credentialsByToken.TryGetValue(token, out var found))
            {
                if (found.ExpiresAt > DateTime.UtcNow)
                {
                    credentials = found;
                    return true;
                }

                _credentialsByToken.Remove(token);
            }

            credentials = null!;
            return false;
        }
    }

    public class FeatsCredentials
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
    }
}