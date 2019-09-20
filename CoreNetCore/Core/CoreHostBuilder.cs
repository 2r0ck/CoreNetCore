using CoreNetCore.Configuration;
using CoreNetCore.Core;
using CoreNetCore.Helpers;
using CoreNetCore.MQ;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace CoreNetCore
{
    public class CoreHostBuilder : IHostBuilder
    {
        private bool _coreHostBuilt;

        public CoreHostBuilder()
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
                services.AddMemoryCache();

                services.AddScoped<IAppId, AppId>();
                services.AddScoped<ICoreConnection, Connection>();
                services.AddScoped<IHealthcheck, Healthcheck>();
                services.AddScoped<ICoreDispatcher, CoreDispatcher>();
                services.AddScoped<IPrepareConfigService, PrepareConfigService>();
                services.AddScoped<IResolver, Resolver>();

                //register core host
                services.AddHostedService<CoreHost>();
            });

            var host = BaseHostBulder.Build();

            var dispatcherHandlers = host.Services.GetServices<IMessageHandler>();

            foreach (var handler in dispatcherHandlers)
            {
                handler.Register();

                var infoHandler = handler as IRegisterHandler;
                if (infoHandler != null)
                {
                    Trace.TraceInformation($"Handler {infoHandler.HandlerName} is registred.");
                }
            }

            return host;
        }

        public IHostBuilder ConfigureAppConfiguration(Action<HostBuilderContext, IConfigurationBuilder> configureDelegate)
        {
            BaseHostBulder.ConfigureAppConfiguration(configureDelegate);
            return this;
        }

        public IHostBuilder ConfigureContainer<TContainerBuilder>(Action<HostBuilderContext, TContainerBuilder> configureDelegate)
        {
            BaseHostBulder.ConfigureContainer(configureDelegate);
            return this;
        }

        public IHostBuilder ConfigureHostConfiguration(Action<IConfigurationBuilder> configureDelegate)
        {
            BaseHostBulder.ConfigureHostConfiguration(configureDelegate);
            return this;
        }

        public IHostBuilder ConfigureServices(Action<HostBuilderContext, IServiceCollection> configureDelegate)
        {
            BaseHostBulder.ConfigureServices(configureDelegate);
            return this;
        }

        public IHostBuilder UseServiceProviderFactory<TContainerBuilder>(IServiceProviderFactory<TContainerBuilder> factory)
        {
            BaseHostBulder.UseServiceProviderFactory(factory);
            return this;
        }
    }
};