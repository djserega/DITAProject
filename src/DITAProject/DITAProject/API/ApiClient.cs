using Newtonsoft.Json;
using RestSharp;
using System.Windows;

namespace ITAJira.API
{
    internal class ApiClient
    {
        private RestClient? _client;
        private string? _userToken;

        internal bool Connect(string password)
        {
            _userToken = default;

            Init();

            IRestRequest request = new RestRequest("api/jira/auth")
                .AddJsonBody(new Models.Auth(Config.Address!, Config.User!, password));

            IRestResponse response = _client!.Post(request);

            ResultMessage result = JsonConvert.DeserializeObject<ResultMessage>(response.Content);

            if (result == default)
                MessageBox.Show("Не вдалось отримати інформацію");
            else if (result.Error)
                MessageBox.Show(result.Message);
            else
                _userToken = result.Message;

            return _userToken != default;
        }

        internal Models.JiraModel.Task[]? GetIssues()
        {
            if (_userToken == default)
                return default;

            IRestRequest request = new RestRequest("api/jira/issues")
                .AddHeader("token", _userToken);

            IRestResponse response = _client!.Get(request);

            ResultMessage result = JsonConvert.DeserializeObject<ResultMessage>(response.Content);

            if (result == default)
                MessageBox.Show("Не вдалось отримати інформацію");
            else if (result.Error)
                MessageBox.Show(result.Message);
            else
            {
                return JsonConvert.DeserializeObject<Models.JiraModel.Task[]>(result.Message);
            }

            return default;
        }


        private void Init()
        {
            if (_client == default)
                _client = new(Config.ApiServer!);
        }
    }

    public class ResultMessage
    {
        public ResultMessage(bool error, string message)
        {
            Error = error;
            Message = message;
        }

        public bool Error { get; set; }
        public string Message { get; set; }
    }
}
