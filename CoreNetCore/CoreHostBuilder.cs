using CoreNetCore.MQ;
using CoreNetCore.Utils;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.Internal;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace CoreNetCore
{
    public class CoreHostBuilder : IHostBuilder
    {
        public const string DEFAULT_UUID_FILE_NAME = "selfUUID.txt";
        public const string CONFIG_KEY_UUID_FILE_NAME = "UUID_FILE_NAME";

        public const string ENVRIOMENT_CONFIG_FILE_NAMES = "CONFIG_FILE_NAME";
        public const string ENVRIOMENT_CONFIG_APP_PREFIX = "GJ_APP_";
        public const string ENVRIOMENT_ENVIRONMENTNAME = "ASPNETCORE_ENVIRONMENT";

        private List<Action<IConfigurationBuilder>> _configureHostConfigActions = new List<Action<IConfigurationBuilder>>();
        private List<Action<HostBuilderContext, IConfigurationBuilder>> _configureAppConfigActions = new List<Action<HostBuilderContext, IConfigurationBuilder>>();
        private List<Action<HostBuilderContext, IServiceCollection>> _configureServicesActions = new List<Action<HostBuilderContext, IServiceCollection>>();

        // Microsoft.Extensions.Hosting.Internal: internal interfaces
        // private List<IConfigureContainerAdapter> _configureContainerActions = new List<IConfigureContainerAdapter>();
        // private Hosting.IServiceFactoryAdapter _serviceProviderFactory = new ServiceFactoryAdapter<IServiceCollection>(new DefaultServiceProviderFactory());

        private bool _coreHostBuilt;

        /// <summary>
        /// Конфигурация хоста (необходим для инициализации переменных окружения IHostingEnvironment)
        /// </summary>
        private IConfiguration _hostConfiguration;

        /// <summary>
        /// Конфигурация приложения (включает конфигурацию хоста)
        /// </summary>
        private IConfiguration _appConfiguration;

        /// <summary>
        /// Главный копонент для связи сервисов при инициализации
        /// </summary>
        private HostBuilderContext _hostBuilderContext;

        /// <summary>
        /// Базовые переменные окружения
        /// </summary>
        private IHostingEnvironment _hostingEnvironment;

        /// <summary>
        /// DI Resolver
        /// </summary>
        private IServiceProvider _appServices;

        public CoreHostBuilder()
        {
            Properties = new Dictionary<object, object>();
        }

        public IDictionary<object, object> Properties { get; }


        public void RunPlatformService(string[] args)
        {
            if (!_coreHostBuilt)
            {
                throw new CoreException("CoreHost not build.");
            }

            var p_service = _appServices.GetService<IPlatformService>();
            if(p_service == null)
            {
                throw new CoreException("No service implementing IPlatformService..");
            }
            p_service.Run(args);
        }

        public IHost Build()
        {
            if (_coreHostBuilt)
            {
                throw new CoreException("Core build can only be called once.");
            }
            _coreHostBuilt = true;

            BuildHostConfiguration();
            CreateHostingEnvironment();
            CreateHostBuilderContext();
            BuildAppConfiguration();
            CreateServiceProvider();

            return new CoreHost(_appServices);
        }

        private void BuildHostConfiguration()
        {
            var configBuilder = new ConfigurationBuilder();
            foreach (var buildAction in _configureHostConfigActions)
            {
                buildAction(configBuilder);
            }
            _hostConfiguration = configBuilder.Build();
        }

        private void CreateHostingEnvironment()
        {
            _hostingEnvironment = new HostingEnvironment()
            {
                ApplicationName = _hostConfiguration[HostDefaults.ApplicationKey] ?? AppDomain.CurrentDomain.FriendlyName,
                EnvironmentName = _hostConfiguration[HostDefaults.EnvironmentKey] ?? Environment.GetEnvironmentVariable(ENVRIOMENT_ENVIRONMENTNAME) ?? EnvironmentName.Production,
                ContentRootPath = ResolveContentRootPath(_hostConfiguration[HostDefaults.ContentRootKey], AppContext.BaseDirectory),
            };
            _hostingEnvironment.ContentRootFileProvider = new PhysicalFileProvider(_hostingEnvironment.ContentRootPath);
        }

        private string ResolveContentRootPath(string contentRootPath, string basePath)
        {
            if (string.IsNullOrEmpty(contentRootPath))
            {
                return basePath;
            }
            if (Path.IsPathRooted(contentRootPath))
            {
                return contentRootPath;
            }
            return Path.Combine(Path.GetFullPath(basePath), contentRootPath);
        }

        private void CreateHostBuilderContext()
        {
            _hostBuilderContext = new HostBuilderContext(Properties)
            {
                HostingEnvironment = _hostingEnvironment,
                Configuration = _hostConfiguration
            };
        }


        private void BuildAppConfiguration()
        {
            var configBuilder = new ConfigurationBuilder();
            configBuilder.AddConfiguration(_hostConfiguration);
            foreach (var buildAction in _configureAppConfigActions)
            {
                buildAction(_hostBuilderContext, configBuilder);
            }
            SetPlatformDefaultConfiguration(configBuilder, _hostingEnvironment.EnvironmentName);
            _appConfiguration = configBuilder.Build();
            _hostBuilderContext.Configuration = _appConfiguration;
        }

        private void SetPlatformDefaultConfiguration(IConfigurationBuilder config, string environmentName)
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
        }

        private void CreateServiceProvider()
        {
            var services = new ServiceCollection();
            services.AddSingleton(_hostingEnvironment);
            services.AddSingleton(_hostBuilderContext);
            services.AddSingleton(_appConfiguration);  
            services.AddOptions();
            services.AddLogging();

            ConfigureCoreServices(_hostBuilderContext, services);

            foreach (var configureServicesAction in _configureServicesActions)
            {
                configureServicesAction(_hostBuilderContext, services);
            }

            _appServices = services.BuildServiceProvider();
            if (_appServices == null)
            {
                throw new CoreException($"The BuildServiceProvider returned a null IServiceProvider.");
            }
        }

        private void ConfigureCoreServices(HostBuilderContext hostBuilderContext, ServiceCollection services)
        {
            services.AddScoped<IAppId, AppId>();
        }

        public IHostBuilder ConfigureAppConfiguration(Action<HostBuilderContext, IConfigurationBuilder> configureDelegate)
        {
            _configureAppConfigActions.Add(configureDelegate ?? throw new ArgumentNullException(nameof(configureDelegate)));
            return this;
        }

        /// <summary>
        /// Для поддержки подключения к другим контейнерам (NotImplemented)
        /// </summary>
        /// <typeparam name="TContainerBuilder"></typeparam>
        /// <param name="configureDelegate"></param>
        /// <returns></returns>
        public IHostBuilder ConfigureContainer<TContainerBuilder>(Action<HostBuilderContext, TContainerBuilder> configureDelegate)
        {
            throw new NotImplementedException("ConfigureContainer not implemented..");
        }

        public IHostBuilder ConfigureHostConfiguration(Action<IConfigurationBuilder> configureDelegate)
        {
            _configureHostConfigActions.Add(configureDelegate ?? throw new ArgumentNullException(nameof(configureDelegate)));
            return this;
        }

        public IHostBuilder ConfigureServices(Action<HostBuilderContext, IServiceCollection> configureDelegate)
        {
            _configureServicesActions.Add(configureDelegate ?? throw new ArgumentNullException(nameof(configureDelegate)));
            return this;
        }

        /// <summary>
        /// Переопределяет фабрику по умолчанию (NotImplemented)
        /// </summary>
        /// <typeparam name="TContainerBuilder"></typeparam>
        /// <param name="factory"></param>
        /// <returns></returns>
        public IHostBuilder UseServiceProviderFactory<TContainerBuilder>(IServiceProviderFactory<TContainerBuilder> factory)
        {
            throw new NotImplementedException("UseServiceProviderFactory not implemented..");
        }
    }
}