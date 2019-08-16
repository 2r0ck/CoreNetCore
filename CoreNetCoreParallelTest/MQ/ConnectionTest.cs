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
            var c = new Core();
            c.Init((sc) => { return sc.AddScoped<IPlatformService, Service1>(); }, "config1.json");
            var resolver = c.ServiceProvider;
            var service = resolver.GetService<IPlatformService>();
            service.Run(null);
        }

        [TestMethod]
        public void RunInstance2()
        {
            var c = new Core();
            c.Init((sc) => { return sc.AddScoped<IPlatformService, Service2>(); }, "config2.json");
            var resolver = c.ServiceProvider;
            var service = resolver.GetService<IPlatformService>();
            service.Run(null);
        }

        [TestMethod]
        public void RunInstance3()
        {
            var hostBuilder = new CoreHostBuilder();

            hostBuilder.ConfigureAppConfiguration((builderContext, configurationBuilder) => configurationBuilder.AddJsonFile("config1.json", true, true))
                       .ConfigureServices((builderContext, services) => services.AddScoped<IPlatformService, Service1>())
                       .Build();

            hostBuilder.RunPlatformService(null);
        }
    }
}