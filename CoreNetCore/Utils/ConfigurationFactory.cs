using Microsoft.Extensions.Configuration;
using System;
using System.Diagnostics;
using System.IO;

namespace CoreNetCore.Utils
{
    public static class ConfigurationFactory
    {
        private static IConfigurationBuilder Configure(IConfigurationBuilder config, string environmentName)
        {
            config.SetBasePath(Directory.GetCurrentDirectory());

            var fileNamesStr = Environment.GetEnvironmentVariable(Core.ENVRIOMENT_CONFIG_FILE_NAMES);
            if (string.IsNullOrEmpty(fileNamesStr))
            {
                fileNamesStr = $"appsettings.json,appsettings.{environmentName}.json";
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

            config.AddEnvironmentVariables(prefix: Core.ENVRIOMENT_CONFIG_APP_PREFIX);
            return config;
        }

        /// <summary>
        /// Get config for .NET Core Console applications.
        /// </summary>
        /// <returns></returns>
        public static IConfiguration CreateConfiguration()
        {
            //Enable if need add aspnet core
            //var env = new HostingEnvironment
            //{
            //    EnvironmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"),
            //    ApplicationName = AppDomain.CurrentDomain.FriendlyName,
            //    ContentRootPath = AppDomain.CurrentDomain.BaseDirectory,
            //    ContentRootFileProvider = new PhysicalFileProvider(AppDomain.CurrentDomain.BaseDirectory)
            //};

            var config = new ConfigurationBuilder();
            var configured = Configure(config, Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"));
            return configured.Build();
        }

       

        #region Use for ASP.NET Core Web applications.

        ///// <summary>
        ///// Use for ASP.NET Core Web applications.
        ///// </summary>
        ///// <param name="config"></param>
        ///// <param name="env"></param>
        ///// <returns></returns>
        //public static IConfigurationBuilder Configure(IConfigurationBuilder config, IHostingEnvironment env)
        //{
        //    return Configure(config, env.EnvironmentName);
        //}

        //private static IConfigurationBuilder Configure(IConfigurationBuilder config, Microsoft.Extensions.Hosting.IHostingEnvironment env)
        //{
        //    return Configure(config, env.EnvironmentName);
        //}

        #endregion Use for ASP.NET Core Web applications.
    }
}