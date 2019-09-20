using CoreNetCore;
using CoreNetCore.MQ;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CoreNetCoreParallelTest.TestServices
{
    public class Service2 : IPlatformService
    {
        public string AppId { get; }
        public IConfiguration Configuration { get; }

        public Service2(IAppId appId, IConfiguration configuration) 
        {
            AppId = appId.CurrentUID;
            Configuration = configuration;
        }

     
        public Task StartAsync(CancellationToken cancellationToken)
        {
            Trace.WriteLine(Configuration["UUID_FILE_NAME"]);
            return Task.CompletedTask;
        }
    }
}
