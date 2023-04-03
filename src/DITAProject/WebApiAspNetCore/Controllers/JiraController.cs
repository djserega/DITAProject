using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using System.Net;
using System.Text.Json;

namespace WebApiAspNetCore.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class JiraController : ControllerBase
    {
        private readonly JiraBus.Main _jiraMain;
        private readonly Logger _logger;

        public JiraController(JiraBus.Main jiraMain, Logger logger)
        {
            _jiraMain = jiraMain;
            _logger = logger;
        }

        [HttpGet]
        public string Get()
        {
            return "Service started at " + DateTime.UtcNow;
        }

        [HttpGet("{id}")]
        public string Get(int id)
        {
            return $"https://djserega.atlassian.net/browse/MAIN-{id}";
        }

        [Route("auth")]
        [HttpPost]
        public async Task<ResultMessage> Post([FromBody] Models.Auth value)
        {
            await Console.Out.WriteLineAsync("Auth");

            ResultMessage resultMessage;

            try
            {
                bool result = await _jiraMain.ConnectUserAsync(value);
                if (result)
                    resultMessage = new(false, _jiraMain.GetUserToken(value.GetHash()) ?? string.Empty);
                else
                    resultMessage = new(true, "Connecting error");
            }
            catch (Exception ex)
            {
                _logger.Err(ex.ToString());
                resultMessage = new(true, ex.Message);
            }

            return resultMessage;
        }

        [Route("issues")]
        [HttpGet]
        public async Task<ResultMessage> GetIssues([FromHeader] string token)
        {
            await Console.Out.WriteLineAsync("issues");

            ResultMessage resultMessage;

            try
            {
                Models.Task[]? issues = _jiraMain.GetIssues(token);

                if (issues == default)
                    return new(false, "");

                return new(false, JsonSerializer.Serialize(issues));
            }
            catch (Exception ex)
            {
                _logger.Err(ex.ToString());
                resultMessage = new(true, ex.Message);
            }

            return resultMessage;
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
}
