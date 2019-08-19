using System.Diagnostics;
using CoreNetCore;
using CoreNetCore.MQ;
using Microsoft.Extensions.Configuration;

namespace CoreNetCoreTest.TestClasses
{
    public class TestService2 : IPlatformService
    {
        public IConfiguration Configuration { get; }

        public TestService2( IConfiguration configuration)  
        {
            Configuration = configuration;
        }     
        

        public   void Run(string[] args)
        {
            Trace.WriteLine($"ServiceIsRunning. Test config key = {Configuration["defaultConfigKey"]}");
        }
    }
}