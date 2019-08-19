using CoreNetCore;
using CoreNetCoreParallelTest.TestServices;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[assembly: Parallelize(Workers = 0, Scope = ExecutionScope.MethodLevel)]

namespace CoreNetCoreParallelTest.MQ
{
    [TestClass]
    public class ConnectionTest
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


        private void RunService<T>(string cfg) where T: class,IPlatformService
        {
            var hostBuilder = new CoreHostBuilder();

            var host = hostBuilder.ConfigureAppConfiguration((builderContext, configurationBuilder) => configurationBuilder.AddJsonFile(cfg, true, true))
                       .ConfigureServices((builderContext, services) => services.AddScoped<IPlatformService, T>())
                       .Build();

            hostBuilder.RunPlatformService(null);
        }


        [TestMethod]
        [DoNotParallelize]
        public void RunInstance3()
        {
            var hostBuilder = new CoreHostBuilder();

            var host = hostBuilder.ConfigureAppConfiguration((builderContext, configurationBuilder) => configurationBuilder.AddJsonFile("config1.json", true, true))
                       .ConfigureServices((builderContext, services) => services.AddScoped<IPlatformService, Service1>())
                       .Build();

            hostBuilder.RunPlatformService(null);
        }

        [TestMethod]
        [DoNotParallelize]
        public void RunInstance4()
        {
            var hostBuilder = new CoreHostBuilder();

            hostBuilder.ConfigureServices((builderContext, services) => services.AddScoped<IPlatformService, Service1>())
                       .Build();

            hostBuilder.RunPlatformService(null);
        }
    }
}