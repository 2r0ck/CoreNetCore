using CoreNetCore.MQ;
using Microsoft.Extensions.Hosting;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace CoreNetCore
{
    internal class CoreHost : BackgroundService
    {
        private readonly IHealthcheck _healthcheck;
        private readonly ICoreConnection _coreConnection;
        private readonly ICoreDispatcher _dispatcher;
        private readonly IServiceProvider _serviceProvider;
        private readonly IApplicationLifetime _appLifetime;
        

        public CoreHost(IServiceProvider serviceProvider, ICoreDispatcher dispatcher, IHealthcheck healthcheck, ICoreConnection coreConnection, IApplicationLifetime appLifetime)
        {
            _appLifetime = appLifetime ?? throw new CoreException("AppLifetime not defined");
            
            _serviceProvider = serviceProvider ?? throw new CoreException("ServiceProvider not defined");  
            _coreConnection = coreConnection ?? throw new CoreException("CoreConnection not defined");
            _healthcheck = healthcheck ?? throw new CoreException("Healthcheck not defined");
            _dispatcher = dispatcher ?? throw new CoreException("CoreDispatcher not defined");

            //SIG handlers
            appLifetime.ApplicationStarted.Register(OnStarted);
            appLifetime.ApplicationStopping.Register(OnStopping);
            appLifetime.ApplicationStopped.Register(OnStopped);
        }

        private void OnStopping()
        {
            _coreConnection.Dispose();
            _healthcheck.Stop();
        }

        private void OnStopped()
        {
            Trace.TraceInformation("Core stopped.");
        }

        private void OnStarted()
        {
            Trace.TraceInformation("All services started.");
        }

        private void _dispatcher_HandleMessageErrors(Models.ReceivedMessageEventArgs msg_ea, Exception ex)
        {
            FatalExit(ex);
        }

        protected void FatalExit(Exception ex)
        {
            Trace.TraceError(ex.ToString());
            Trace.TraceInformation($"FatalExit(1)");
            Environment.ExitCode = 1;
            _appLifetime.StopApplication();
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            AutoResetEvent continueStart = new AutoResetEvent(false);

            _dispatcher.Started += (appid) =>
            {
                Trace.TraceInformation($"Dispatcher [{appid}] started.");
                continueStart.Set();
            };
            _dispatcher.HandleMessageErrors += _dispatcher_HandleMessageErrors;

            _coreConnection.Start();
            continueStart.WaitOne();


            var platformService = _serviceProvider.GetService<IPlatformService>();

            if (platformService != null)
            {
                return Task.WhenAny(_healthcheck.StartAsync(), platformService.StartAsync(stoppingToken));
            }
            else //run without service
            {
                return _healthcheck.StartAsync();
            }

            
        }
    }
}