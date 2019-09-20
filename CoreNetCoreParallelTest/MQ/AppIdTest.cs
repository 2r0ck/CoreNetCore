using CoreNetCore;
using CoreNetCoreParallelTest.TestServices;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading;

namespace CoreNetCoreParallelTest.MQ
{
    [TestClass]
    public class AppIdTest
    {
        [TestMethod]
        public void RunInstance1()
        {
            RunService<Service2>("config1.json");
        }

        [TestMethod]
        public void RunInstance2()
        {
            RunService<Service2>("config2.json");
        }

        private void RunService<T>(string cfg) where T : class, IPlatformService
        {
            var hostBuilder = new CoreHostBuilder();

            var host = hostBuilder.ConfigureAppConfiguration((builderContext, configurationBuilder) => configurationBuilder.AddJsonFile(cfg, true, true))
                       .ConfigureServices((builderContext, services) => services.AddScoped<IPlatformService, T>())
                       .Build();

            host.Services.GetService<IPlatformService>().StartAsync(default(CancellationToken));
        }
    }
}