using System;
using System.Collections.Generic;
using System.Text;
using CoreNetCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Extensions.DependencyInjection;
using CoreNetCoreTest.TestClasses;
using System.IO;
using Microsoft.Extensions.Configuration;
using CoreNetCore.MQ;
using CoreNetCore.Utils;

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

    }
}
