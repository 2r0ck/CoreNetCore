using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace CoreNetCore.Configuration
{
    public class ConfigurationFactory
    {
        public const string ENVRIOMENT_CONFIG_FILE_NAMES = "CONFIG_FILE_NAME";
        public const string ENVRIOMENT_CONFIG_APP_PREFIX = "GJ_APP_";

        public ConfigurationFactory(IHostingEnvironment hostingEnvironment)
        {
            HostingEnvironment = hostingEnvironment;
        }

        public IHostingEnvironment HostingEnvironment { get; }

        public IConfiguration GetDefault()
        {
            var config = new ConfigurationBuilder();
            var configured = ConfigureDefault(config);
            return configured.Build();
        }

        private IConfigurationBuilder ConfigureDefault(IConfigurationBuilder config)
        {
            config.SetBasePath(Directory.GetCurrentDirectory());

            var fileNamesStr = Environment.GetEnvironmentVariable(ConfigurationFactory.ENVRIOMENT_CONFIG_FILE_NAMES);
            if (string.IsNullOrEmpty(fileNamesStr))
            {
                fileNamesStr = $"appConfig.json,appConfig.{HostingEnvironment.EnvironmentName}.json";                
            }

            var cfgFiles = fileNamesStr.Split(',', StringSplitOptions.RemoveEmptyEntries);

            foreach (var file in cfgFiles)
            {
                if (".json".Equals(Path.GetExtension(file)))
                {
                    config.AddJsonFile(file, true, true);
                    Trace.TraceInformation($"Load config file: {file}");
                }

                if (".xml".Equals(Path.GetExtension(file)) || ".config".Equals(Path.GetExtension(file)))
                {
                    config.AddXmlFile(file, true, true);
                    Trace.TraceInformation($"Load config file: {file}");
                }
            }

            config.AddEnvironmentVariables(prefix: ConfigurationFactory.ENVRIOMENT_CONFIG_APP_PREFIX);
            return config;
        }
    }
}
