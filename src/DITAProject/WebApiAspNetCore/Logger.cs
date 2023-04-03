using Serilog;

namespace WebApiAspNetCore
{
    public class Logger
    {
        private readonly Serilog.Core.Logger? _seriLog = null;
        private readonly Config _config;

        public Logger(Config config)
        {
            _config = config;

            if (_config.Debug)
            {
                _seriLog = new LoggerConfiguration()
                    .MinimumLevel.Verbose()
                    .WriteTo.File("log.txt")
                    .CreateLogger();

                Inf("Logger starting");
            }
        }

        public void Inf(string text)
        {
            if (_config.Debug)
                _seriLog?.Information(text);
        }
        public void Inf(string text, params object[] propertyValues)
        {
            if (_config.Debug)
                _seriLog?.Information(text, propertyValues);
        }

        public void Err(Exception ex)
        {
            if (_config.Debug)
                _seriLog?.Error(ex.ToString());
        }
        public void Err(string text)
        {
            if (_config.Debug)
                _seriLog?.Error(text);
        }
        public void Err(string text, params object[] propertyValues)
        {
            if (_config.Debug)
                _seriLog?.Information(text, propertyValues);
        }

    }
}
