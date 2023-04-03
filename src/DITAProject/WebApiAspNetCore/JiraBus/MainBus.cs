namespace WebApiAspNetCore.JiraBus
{
    public class Main
    {
        private readonly Dictionary<int, JiraConnector> _userConnectors = new();
        private readonly Dictionary<string, int> _userTokens = new();

        private readonly Config _config;
        private readonly Logger _logger;

        public Main(Config config, Logger logger)
        {
            _config = config;
            _logger = logger;
        }

        internal async Task<bool> ConnectUserAsync(Models.Auth auth)
        {
            int currentHash = auth.GetHash();

            if (_userConnectors.ContainsKey(currentHash))
            {
                _userConnectors.Remove(currentHash);
            }

            if (auth.CheckFields())
            {
                JiraConnector connector = new(_config, _logger)
                {
                    Address = auth.Server,
                    User = auth.User
                };
                await connector.ConnectAsync(auth.Password!);

                if (connector.IsConnected)
                {
                    _userConnectors.Add(currentHash, connector);
                    _userTokens.Add(Guid.NewGuid().ToString().Replace("-", ""), currentHash);
                    return true;
                }
            }

            return false;
        }

        internal Models.Task[]? GetIssues(string token)
        {
            JiraConnector? connector = GetConnectorByToken(token);

            if (connector == default)
                return default;

            return connector.GetTask()?.ToArray();

        }

        private JiraConnector? GetConnectorByToken(string token)
        {
            if (_userTokens.TryGetValue(token, out int userId))
                if (_userConnectors.TryGetValue(userId, out JiraConnector? connector))
                    return connector;

            return default;
        }

        internal string? GetUserToken(int userId)
        {
            string? token = default;

            foreach (KeyValuePair<string, int> itemToken in _userTokens)
            {
                if (itemToken.Value == userId)
                {
                    token = itemToken.Key;
                }
            }

            return token;
        }
    }
}
