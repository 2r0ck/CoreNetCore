using CoreNetCore.MQ;
using CoreNetCore.Utils;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Diagnostics;

namespace CoreNetCore
{
    public class Core
    {
        #region Const

        public const string DEFAULT_UUID_FILE_NAME = "selfUUID.txt";
        public const string CONFIG_KEY_UUID_FILE_NAME = "UUID_FILE_NAME";

        public const string ENVRIOMENT_CONFIG_FILE_NAMES = "CONFIG_FILE_NAME";
        public const string ENVRIOMENT_CONFIG_APP_PREFIX = "GJ_APP_";

        #endregion Const

        //public static Core Current => _instance;

        //internal static readonly Core _instance = new Core();

        //private Core()
        //{
        //}

        private ServiceProvider serviceProvider;

        /// <summary>
        /// DI resolver for core
        /// </summary>
        public ServiceProvider ServiceProvider
        {
            get
            {
                if (serviceProvider == null)
                {
                    throw new CoreException("ServiceProvider not initialized. Please, call init method.");
                }
                return serviceProvider;
            }

            private set
            {
                serviceProvider = value;
            }
        }

        /// <summary>
        /// Initialize bases services.
        /// </summary>
        /// <param name="customConfiguredFunc">Func for custom configured DI</param>
        /// <param name="configFilePath">Config file name</param>
        /// <remarks>
        /// Config path priority:
        /// 1) configFilePath (for Testing)
        /// 2) ENVRIOMENT_CONFIG_FILE_NAMES
        /// 3) appsettings.json,appsettings.{environmentName}.json (by Default)
        /// </remarks>
        public void Init(Func<IServiceCollection, IServiceCollection> customConfiguredFunc = null, string configFilePath = null)
        {
            var sCollection = new ServiceCollection()
                            .AddSingleton<IConfiguration>(provider => ConfigurationFactory.CreateConfiguration(configFilePath))
                            .AddScoped<IAppId, AppId>();
            if (customConfiguredFunc != null)
            {
                sCollection = customConfiguredFunc(sCollection);
            }

            serviceProvider = sCollection.BuildServiceProvider();
            Trace.TraceInformation("Core initialized successfully.");
        }
    }
}