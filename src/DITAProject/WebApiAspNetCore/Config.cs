using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace WebApiAspNetCore
{
    public class Config
    {
        private const string _nameConfigJson = "config.json";

        internal static string BaseDirectory { get => new FileInfo(Assembly.GetExecutingAssembly().Location).Directory?.FullName ?? string.Empty; }

        public Config()
        {
            IConfigurationRoot? _config = new ConfigurationBuilder()
                    .SetBasePath(BaseDirectory)
                    .AddJsonFile(_nameConfigJson, false, true)
                    .Build();

            Debug = _config.GetValue<bool>("debug");

            Address = _config.GetValue<string>("address");
            User = _config.GetValue<string>("user");
            Projects[0] = _config.GetValue<string>("projects:1") ?? string.Empty;
            Projects[1] = _config.GetValue<string>("projects:2") ?? string.Empty;
        }

        internal string NameConfig { get => _nameConfigJson; }

        internal bool Debug { get; }

        internal string? Address { get; }
        internal string? User { get; }
        internal string[] Projects { get; } = new string[2];
    }
}
