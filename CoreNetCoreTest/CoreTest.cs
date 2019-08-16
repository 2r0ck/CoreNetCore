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
            Core.Current.Init(sc => { return sc.AddScoped<ITestService, TestService>(); });
            var resolver = Core.Current.ServiceProvider;
            var test = resolver.GetService<ITestService>();
            Assert.IsNotNull(test);
        }

        [TestMethod]
        public void DI_Scoped_Test()
        {
            Core.Current.Init(sc => { return sc.AddScoped<ITestService, TestService>(); });
            var resolver = Core.Current.ServiceProvider;

            var test1 = resolver.GetService<ITestService>();
            var test2 = resolver.GetService<ITestService>();
            Assert.AreSame(test1, test2);
            Assert.AreEqual(test1.TID, test2.TID);
        }

        [TestMethod]
        public void DI_Transient_Test()
        {
            Core.Current.Init(sc => { return sc.AddTransient<ITestService, TestService>(); });
            var resolver = Core.Current.ServiceProvider;
            var test1 = resolver.GetService<ITestService>();
            var test2 = resolver.GetService<ITestService>();
            Assert.AreNotSame(test1, test2);
            Assert.AreNotEqual(test1.TID, test2.TID);
        }

        [TestMethod]
        public void DI_TestBaseRunning()
        {
            Core.Current.Init(sc => { return sc.AddTransient<BaseService, TestService2>(); });
            var resolver = Core.Current.ServiceProvider;
            var service = resolver.GetService<BaseService>();
            var id = service.AppId;

            IConfiguration configuration = resolver.GetService<IConfiguration>();
            var res = resolver.GetService<IAppId>();

            var filename = configuration.GetStrValue(Core.CONFIG_KEY_UUID_FILE_NAME);

            if (string.IsNullOrEmpty(filename))
            {
                filename = Core.DEFAULT_UUID_FILE_NAME;
            }

            //check id
            if (File.Exists(filename))
            {
                var fileContent = File.ReadAllText(filename);
                Assert.AreEqual(id, fileContent);
            }
            else
            {
                Assert.Fail("UUID_FILE not found!");
            }

            service.Run(null);

            Assert.IsTrue(true);
        }

        [TestMethod]
        public void DI_DoubleScope_Test()
        {
            Core.Current.Init(sc =>
            {
                return sc
                        .AddScoped<ITestService, TestService_One>()
                        .AddScoped<ITestService, TestService_Second>();
            });
            var resolver = Core.Current.ServiceProvider;
            var test1 = resolver.GetService<ITestService>();
            Trace.WriteLine(test1.TID);
        }
    }
}