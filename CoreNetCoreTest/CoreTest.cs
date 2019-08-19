using CoreNetCore;
using CoreNetCore.MQ;
using CoreNetCore.Utils;
using CoreNetCoreTest.TestClasses;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Diagnostics;
using System.IO;

namespace CoreNetCoreTest
{
    [TestClass]
    public class CoreTest
    {
        //todo: test DI
        //test create service

        [TestMethod]
        public void DI_Test()
        {            
            var hostBuilder = new CoreHostBuilder();

            var host = hostBuilder
                       .ConfigureServices((builderContext, services) => services.AddScoped<ITestService, TestService>())
                       .Build();

            var test = host.Services.GetService<ITestService>();
            Assert.IsNotNull(test);
        }

        [TestMethod]
        public void DI_Scoped_Test()
        {

            var hostBuilder = new CoreHostBuilder();

            var host = hostBuilder
                       .ConfigureServices((builderContext, services) => services.AddScoped<ITestService, TestService>())
                       .Build();
            var test1 = host.Services.GetService<ITestService>();
            var test2 = host.Services.GetService<ITestService>();
            
            Assert.AreSame(test1, test2);
            Assert.AreEqual(test1.TID, test2.TID);
        }

        [TestMethod]
        public void DI_Transient_Test()
        {
            var hostBuilder = new CoreHostBuilder();

            var host = hostBuilder
                       .ConfigureServices((builderContext, services) => services.AddTransient<ITestService, TestService>())
                       .Build();
            var test1 = host.Services.GetService<ITestService>();
            var test2 = host.Services.GetService<ITestService>();
            Assert.AreNotSame(test1, test2);
            Assert.AreNotEqual(test1.TID, test2.TID);
        }

        [TestMethod]
        public void DI_TestBaseRunning()
        {
            var hostBuilder = new CoreHostBuilder();

            var host = hostBuilder
                       .ConfigureServices((builderContext, services) => services.AddTransient<IPlatformService, TestService2>())
                       .Build();

            var resolver = host.Services;
            var service = resolver.GetService<IPlatformService>();

            IConfiguration configuration = resolver.GetService<IConfiguration>();
            var res = resolver.GetService<IAppId>();

            var filename = configuration.GetStrValue(AppId.CONFIG_KEY_UUID_FILE_NAME);

            if (string.IsNullOrEmpty(filename))
            {
                filename = AppId.DEFAULT_UUID_FILE_NAME;
            }

            //check id
            if (File.Exists(filename))
            {
                var fileContent = File.ReadAllText(filename);
                Assert.AreEqual(res.CurrentUID, fileContent);
            }
            else
            {
                Assert.Fail("UUID_FILE not found!");
            }

            service.Run(null);

            Assert.IsTrue(true);
        }
        
    }
}