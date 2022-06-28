using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ITAJira
{

    internal static class Config
    {
        private const string _nameConfigJson = "config.json";

        static Config()
        {
            IConfigurationRoot? _config = new ConfigurationBuilder()
                    .SetBasePath(new FileInfo(Assembly.GetExecutingAssembly().Location).Directory?.FullName)
                    .AddJsonFile(_nameConfigJson, false, true)
                    .Build();

            Debug = _config.GetValue<bool>("debug");

            Address = _config.GetValue<string>("address");
            User = _config.GetValue<string>("user");
            Project = _config.GetValue<string>("project");
        }

        internal static string NameConfig { get => _nameConfigJson; }

        internal static bool Debug { get; }

        internal static string Address { get; }
        internal static string User { get; }
        internal static string Project { get; }
    }
}
