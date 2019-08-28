using CoreNetCore.MQ;
using Microsoft.Extensions.Hosting;
using System;
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
            connection.Start();
            await healthck.StartAsync();
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