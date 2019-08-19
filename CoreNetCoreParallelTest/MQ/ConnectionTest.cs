using CoreNetCore;
using CoreNetCore.MQ;
using CoreNetCoreParallelTest.TestServices;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Diagnostics;
using System.IO;

[assembly: Parallelize(Workers = 0, Scope = ExecutionScope.MethodLevel)]

namespace CoreNetCoreParallelTest.MQ
{
    [TestClass]
    public class ConnectionTest
    {
        [TestMethod]
        public void FanoutInstance1()
        {
            RunService<FanoutService1>("config1.json", new[] { "service1", "service2", "Query1" });
            Trace.Flush();
        }

        [TestMethod]
        public void FanoutInstance2()
        {
            RunService<FanoutService1>("config2.json", new[] { "service2", "service1" });
        }

        [TestMethod]
        public void DirectInstance1()
        {
            var service2Id = File.ReadAllText("UUID_direct2.txt");


            RunService<DirectService1>("configDirect1.json", new[] { "service1", "service2", service2Id,"Query1" });
        }

        [TestMethod]
        public void DirectInstance2()
        {
            var service1Id = File.ReadAllText("UUID_direct1.txt");
            RunService<DirectService1>("configDirect2.json", new[] { "service2", "service1", service1Id });
        }


        

        private void RunService<T>(string cfg, string[] args) where T : class, IPlatformService
        {
            var hostBuilder = new CoreHostBuilder();

            var host = hostBuilder.ConfigureAppConfiguration((builderContext, configurationBuilder) => configurationBuilder.AddJsonFile(cfg, true, true))
                       .ConfigureServices((builderContext, services) =>
                       {
                           services.AddScoped<IPlatformService, T>();
                       }
                       )
                       .Build();

            hostBuilder.RunPlatformService(args);
        }
    }
}