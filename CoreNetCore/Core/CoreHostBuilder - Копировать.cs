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
using Microsoft.Extensions.Configuration.Json;
using CoreNetCore.Core;

namespace CoreNetCore
{
    public class CoreHostBuilder1 : IHostBuilder
    {

        bool _coreHostBuilt;
         
        public CoreHostBuilder1()
        {
            Properties = new Dictionary<object, object>();
            BaseHostBulder = new HostBuilder();
        }

        public IDictionary<object, object> Properties { get; }
        public HostBuilder BaseHostBulder { get; }

        public IHost Build()
        {
            if (_coreHostBuilt)
            {
                throw new CoreException("Core build can only be called once.");
            }
            _coreHostBuilt = true;

            BaseHostBulder.ConfigureAppConfiguration((hostContext, configBuilder) =>
            {

                configBuilder.AddConfiguration(new ConfigurationFactory(hostContext.HostingEnvironment).GetDefault());
            });

            BaseHostBulder.ConfigureServices((services) =>
            {
                services.AddScoped<IAppId, AppId>();
                services.AddScoped<ICoreConnection, Connection>();
                services.AddScoped<IHealthcheck, Healthcheck>();
                services.AddScoped<ICoreDispatcher, CoreDispatcher>();
                services.AddScoped<IPrepareConfigService, PrepareConfigService>();
                services.AddScoped<IResolver, Resolver>();
            });

            var host = BaseHostBulder.Build();

            var dispatcherHandlers = host.Services.GetServices<IMessageHandler>();

            foreach (var handler in dispatcherHandlers)
            {
                handler.Register();
            }
            
            
            //register IHostedService
            //On Start & on stop call IplatformService events (wait all)!!!!!

        }

        public IHostBuilder ConfigureAppConfiguration(Action<HostBuilderContext, IConfigurationBuilder> configureDelegate)
        {
            return BaseHostBulder.ConfigureAppConfiguration(configureDelegate);
        }

        public IHostBuilder ConfigureContainer<TContainerBuilder>(Action<HostBuilderContext, TContainerBuilder> configureDelegate)
        {
            return BaseHostBulder.ConfigureContainer(configureDelegate);
        }

        public IHostBuilder ConfigureHostConfiguration(Action<IConfigurationBuilder> configureDelegate)
        {
            return BaseHostBulder.ConfigureHostConfiguration(configureDelegate);
        }

        public IHostBuilder ConfigureServices(Action<HostBuilderContext, IServiceCollection> configureDelegate)
        {
            return BaseHostBulder.ConfigureServices(configureDelegate);
        }

        public IHostBuilder UseServiceProviderFactory<TContainerBuilder>(IServiceProviderFactory<TContainerBuilder> factory)
        {
            return BaseHostBulder.UseServiceProviderFactory(factory);
        }
    }
};