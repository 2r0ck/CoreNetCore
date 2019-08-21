using CoreNetCore.Configuration;
using CoreNetCore.MQ;
using CoreNetCore.Utils;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.Internal;
using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.Logging;

namespace CoreNetCore
{
    public class CoreHostBuilder : IHostBuilder
    {
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
            if (p_service == null)
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
            //set default configuration
            configBuilder.AddConfiguration(new ConfigurationFactory(_hostingEnvironment).GetDefault());

            _appConfiguration = configBuilder.Build();
            _hostBuilderContext.Configuration = _appConfiguration;
        }

        private void CreateServiceProvider()
        {
            var services = new ServiceCollection();
            services.AddSingleton(_hostingEnvironment);
            services.AddSingleton(_hostBuilderContext);
            services.AddSingleton(_appConfiguration);
            services.AddOptions();
            services.AddLogging();
            //services.AddHttpClient();
         

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

#pragma warning disable RECS0154 // Parameter is never used
        private void ConfigureCoreServices(HostBuilderContext hostBuilderContext, ServiceCollection services)
#pragma warning restore RECS0154 // Parameter is never used
        {
            services.AddScoped<IAppId, AppId>();
            services.AddScoped<ICoreConnection, Connection>();
            services.AddScoped<IHealthcheck, Healthcheck>();
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