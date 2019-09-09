using CoreNetCore.MQ;
using Microsoft.Extensions.Hosting;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace CoreNetCore
{
    internal class CoreHost : IHost
    {
        public CoreHost(IServiceProvider services)
        {
            Services = services;
        }

        public IServiceProvider Services { get; }

        public void Dispose()
        {
        }

        //TODO: посмотри, тут какая то херня(возможн) and StopAsync
        public async Task StartAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            await Task.Run(() =>
            {
                AutoResetEvent continueStart = new AutoResetEvent(false);

                var healthck = this.GetService<IHealthcheck>();
                if (healthck == null)
                {
                    throw new CoreException("Healthcheck not defined");
                }

                var connection = this.GetService<ICoreConnection>();
                if (connection == null)
                {
                    throw new CoreException("CoreConnection not defined");
                }

               //????/
               var dispatcher = this.GetService<ICoreDispatcher>();
                if (dispatcher == null)
                {
                    throw new CoreException("CoreDispatcher not defined");
                }

                dispatcher.Started += (appid) =>
                {
                    Trace.TraceInformation($"Dispatcher [{appid}] started.");
                    continueStart.Set();
                };

                connection.Start();
                healthck.StartAsync();
                continueStart.WaitOne();
            });
        }

        public async Task StopAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            await Task.Run(() =>
            {
                var healthck = this.GetService<IHealthcheck>();
                if (healthck == null)
                {
                    throw new CoreException("Healthcheck not defined");
                }

                var connection = this.GetService<ICoreConnection>();
                if (connection == null)
                {
                    throw new CoreException("CoreConnection not defined");
                }

                connection.Dispose();
                healthck.Stop();
            });
        }
    }
}