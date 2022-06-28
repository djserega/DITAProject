using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ITAJira
{
    internal class Logger
    {
        private static readonly Serilog.Core.Logger? _seriLog = null;

        static Logger()
        {
            if (Config.Debug)
            {
                _seriLog = new LoggerConfiguration()
                    .MinimumLevel.Verbose()
                    .WriteTo.File("log.txt")
                    .CreateLogger();

                Inf("Logger starting");
            }
        }

        public static void Inf(string text)
        {
            if (Config.Debug)
                _seriLog?.Information(text);
        }
        public static void Inf(string text, params object[] propertyValues)
        {
            if (Config.Debug)
                _seriLog?.Information(text, propertyValues);
        }

        public static void Err(string text)
        {
            if (Config.Debug)
                _seriLog?.Error(text);
        }
        public static void Err(string text, params object[] propertyValues)
        {
            if (Config.Debug)
                _seriLog?.Information(text, propertyValues);
        }

    }
}
