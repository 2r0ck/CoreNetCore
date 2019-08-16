using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Text;
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

        public Task StartAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        public Task StopAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException();
        }
    }
}
