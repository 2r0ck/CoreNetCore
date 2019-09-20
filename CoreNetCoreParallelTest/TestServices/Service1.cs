using CoreNetCore;
using CoreNetCore.MQ;
using Microsoft.Extensions.Configuration;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace CoreNetCoreParallelTest.TestServices
{
    public class Service1 : IPlatformService
    {
        public String AppId { get; }

        public Service1(IAppId appId, IConfiguration configuration)
        {
            AppId = appId.CurrentUID;
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            Trace.WriteLine(Configuration["UUID_FILE_NAME"]);
            return Task.CompletedTask;
        }
    }
}