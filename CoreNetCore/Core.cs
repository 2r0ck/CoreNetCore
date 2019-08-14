using CoreNetCore.MQ;
using CoreNetCore.Utils;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Diagnostics;

namespace CoreNetCore
{
    public sealed class Core
    {
        #region Const

        public const string DEFAULT_UUID_FILE_NAME = "selfUUID.txt";
        public const string CONFIG_KEY_UUID_FILE_NAME = "UUID_FILE_NAME";

        public const string ENVRIOMENT_CONFIG_FILE_NAMES = "CONFIG_FILE_NAME";
        public const string ENVRIOMENT_CONFIG_APP_PREFIX = "GJ_APP_";

        #endregion Const

        public static Core Current => _instance;

        internal static readonly Core _instance = new Core();

        private Core()
        {
        }

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
        public void Init(Func<IServiceCollection, IServiceCollection> customConfiguredFunc = null)
        {
            var sCollection = new ServiceCollection()

                            .AddSingleton<IConfiguration>(provider => ConfigurationFactory.CreateConfiguration())
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