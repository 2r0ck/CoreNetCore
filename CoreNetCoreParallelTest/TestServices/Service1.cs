using CoreNetCore;
using CoreNetCore.MQ;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace CoreNetCoreParallelTest.TestServices
{
    public class Service1 : IPlatformService
    {
        public String AppId { get; }

        public Service1(IAppId appId ,IConfiguration configuration)  
        {
            AppId = appId.CurrentUID;
            Configuration = configuration;
            
        }

        public IConfiguration Configuration { get; }

        public  void Run(string[] args)
        {
            Trace.WriteLine(Configuration["UUID_FILE_NAME"]);
        }
    }
}
