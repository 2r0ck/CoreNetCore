using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
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
        public Task StartAsync(CancellationToken cancellationToken)
        {
            Trace.WriteLine($"ServiceIsRunning. Test config key = {Configuration["defaultConfigKey"]}");
            return Task.CompletedTask;
        }
    }
}