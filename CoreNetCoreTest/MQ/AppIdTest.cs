using CoreNetCore;
using CoreNetCore.MQ;
using CoreNetCore.Utils;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;

namespace CoreNetCoreTest.MQ
{
    [TestClass]
    public class AppIdTest
    {
        private IServiceProvider serviceProvider;

        [TestInitialize]
        public void Init()
        {
            var hostBuilder = new CoreHostBuilder();
            var host = hostBuilder.Build();
            serviceProvider = host.Services;
        }

        [TestMethod]
        public void Scope_Test()
        {
            var app = serviceProvider.GetService<AppId>();

            var app2 = serviceProvider.GetService<AppId>();
            Assert.AreEqual(app, app2);
        }

        [TestMethod]
        public void GetAppId_Test()
        {
            var app = serviceProvider.GetService<IAppId>();
            Assert.IsNotNull(app.CurrentUID);
        }

        [TestMethod]
        public void GetPersistentAppId_Test()
        {
            var app = serviceProvider.GetService<IAppId>();
            var uid = app.CurrentUID;

            IConfiguration configuration = serviceProvider.GetService<IConfiguration>();

            var filename = configuration.GetStrValue(AppId.CONFIG_KEY_UUID_FILE_NAME);

            if (string.IsNullOrEmpty(filename))
            {
                filename = AppId.DEFAULT_UUID_FILE_NAME;
            }

            if (File.Exists(filename))
            {
                var fileContent = File.ReadAllText(filename);
                Assert.AreEqual(fileContent, uid);
            }
            else
            {
                Assert.Fail("UUID_FILE not found");
            }
        }
    }
}