using System.Diagnostics;
using CoreNetCore;
using CoreNetCore.MQ;
using Microsoft.Extensions.Configuration;

namespace CoreNetCoreTest.TestClasses
{
    public class TestService2 : BaseService
    {
        public TestService2(IAppId appId, IConfiguration configuration) : base(appId, configuration)
        {
        }     
        

        public override void Run(string[] args)
        {
            Trace.WriteLine($"ServiceIsRunning. Test config key = {Configuration["defaultConfigKey"]}");
        }
    }
}