using CoreNetCore;
using CoreNetCore.MQ;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

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

        public  void Run(string[] args)
        {
            Trace.WriteLine(Configuration["UUID_FILE_NAME"]);
        }
    }
}
