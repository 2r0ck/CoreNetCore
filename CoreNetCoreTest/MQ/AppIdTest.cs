using System.IO;
using CoreNetCore;
using CoreNetCore.MQ;
using CoreNetCore.Utils;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CoreNetCoreTest.MQ
{
    [TestClass]
    public class AppIdTest
    {
        private ServiceProvider serviceProvider;
      

        [TestInitialize]
        public void Init()
        {
            //Set variables
            //var files = Directory.GetFiles(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\Config"));
            //Environment.SetEnvironmentVariable(ConfigurationFactory.ENVRIOMENT_CONFIG_FILE_NAME, string.Join(",", files));

            Core.Current.Init();
            serviceProvider = Core.Current.ServiceProvider;            
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

            var filename = configuration.GetStrValue(Core.CONFIG_KEY_UUID_FILE_NAME);

            if (string.IsNullOrEmpty(filename))
            {
                filename = Core.DEFAULT_UUID_FILE_NAME;
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