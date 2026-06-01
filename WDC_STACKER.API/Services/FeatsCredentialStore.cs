namespace WDC_STACKER.API.Services
{
    public class FeatsCredentialStore
    {
        private readonly Dictionary<string, FeatsCredentials> _credentialsByToken = new();

        public void Store(string token, string username, string password)
        {
            _credentialsByToken[token] = new FeatsCredentials
            {
                Username = username,
                Password = password
            };
        }

        public bool TryGet(string token, out FeatsCredentials credentials)
        {
            return _credentialsByToken.TryGetValue(token, out credentials!);
        }
    }

    public class FeatsCredentials
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}