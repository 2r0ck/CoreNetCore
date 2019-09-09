using CoreNetCore;
using CoreNetCore.Configuration;
using CoreNetCore.Models;
using CoreNetCore.Utils;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.IO;

namespace CoreNetCoreTest.Utils
{
    [TestClass]
    public class ConfigTest
    {
        public object Traceitem { get; private set; }

        [TestMethod]
        public void DefaultConfigFile_Test()
        {
            var hostBuilder = new CoreHostBuilder();
            var host = hostBuilder.Build();
            var factory = host.Services.GetService<IConfiguration>();

            var testValue = factory.GetStrValue("defaultConfigKey");
            Assert.AreEqual("test value", testValue);
        }

        [TestMethod]
        public void CustomConfigFiles_Test()
        {
            //Set variables
            var files = Directory.GetFiles(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\TestConfig"));
            Environment.SetEnvironmentVariable(ConfigurationFactory.ENVRIOMENT_CONFIG_FILE_NAMES, string.Join(",", files));

            var hostBuilder = new CoreHostBuilder();
            var host = hostBuilder.Build();
            var factory = host.Services.GetService<IConfiguration>();

            //read json config
            //1
            var testValue = factory.GetStrValue("testJsonConfigKey");
            Assert.AreEqual("jsonValue", testValue);
            //2
            var testValue2 = factory["parentJsonKey:childJsonKey"];
            Assert.AreEqual("ValuehildJsonKey", testValue2);

            //read xml config
            //1
            var testxmlvalue = factory.GetStrValue("testXmlKey");
            Assert.AreEqual("xmlValue", testxmlvalue);
            //2
            var childXmlKeyValue = factory["parentXmlKey:childXmlKey"];
            Assert.AreEqual("childXmlKeyValue", childXmlKeyValue);
        }

        [TestMethod]
        public void EnvironmentConfig_Test()
        {
            //Set variables
            var testEnvriomentName = "TEST_ENV_NAME";
            var testEnvriomentValue = "TEST_ENV_NAME VALUE";

            Environment.SetEnvironmentVariable(ConfigurationFactory.ENVRIOMENT_CONFIG_APP_PREFIX + testEnvriomentName, testEnvriomentValue);

            var hostBuilder = new CoreHostBuilder();
            var host = hostBuilder.Build();
            var factory = host.Services.GetService<IConfiguration>();

            Assert.AreEqual(factory[testEnvriomentName], testEnvriomentValue);
        }

        [TestMethod]
        public void EnvironmentRewriteConfig_Test()
        {
            var testJSONAndEnvriomentKey = "envCustomTest";
            var testEnvriomentValue = "envCustomTest ENV VALUE";
            //var testJSONValue = "EnvCustomTest JSON Value";

            //Set cfg
            var files = Directory.GetFiles(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\TestConfig"));
            Environment.SetEnvironmentVariable(ConfigurationFactory.ENVRIOMENT_CONFIG_FILE_NAMES, string.Join(",", files));

            //Set variables
            Environment.SetEnvironmentVariable(ConfigurationFactory.ENVRIOMENT_CONFIG_APP_PREFIX + testJSONAndEnvriomentKey, testEnvriomentValue);

            var hostBuilder = new CoreHostBuilder();
            var host = hostBuilder.Build();
            var factory = host.Services.GetService<IConfiguration>();

            //rewrite
            Assert.AreEqual(factory[testJSONAndEnvriomentKey], testEnvriomentValue);
        }

        [TestMethod]
        public void GetSectionTest()
        {
            CfgStarterSection cfg_starter = new CfgStarterSection();
            var hostBuilder = new CoreHostBuilder();
            var host = hostBuilder.Build();
            var configuration = host.Services.GetService<IConfiguration>();
            configuration.GetSection("starter").Bind(cfg_starter, options => options.BindNonPublicProperties = true);

            Assert.IsNotNull(cfg_starter?._this?.servicename);
        }

        [TestMethod]
        public void GetPrepareConfigTest()
        {
            var host = new CoreHostBuilder().Build();
            var configuration = host.Services.GetService<IPrepareConfigService>();
            Assert.IsNotNull(configuration?.MQ);
            Assert.IsNotNull(configuration?.Starter);
        }

        [TestMethod]
        public void ValidateSectionTest()
        {
            CfgStarterSection cfg_starter = new CfgStarterSection();
            var hostBuilder = new CoreHostBuilder();
            var host = hostBuilder.Build();
            var configuration = host.Services.GetService<IConfiguration>();
            configuration.GetSection("starter").Bind(cfg_starter, options => options.BindNonPublicProperties = true);
            Assert.IsTrue(cfg_starter.Validate());
        }



        [TestMethod]
        public void ValidateSectionTest2()
        {
            CfgStarterSection cfg_starter = new CfgStarterSection();
            var hostBuilder = new CoreHostBuilder();
            var host = hostBuilder.Build();
            var configuration = host.Services.GetService<IConfiguration>();
            configuration.GetSection("starter").Bind(cfg_starter, options => options.BindNonPublicProperties = true);
            Assert.IsTrue(cfg_starter.Validate());

            cfg_starter._this.servicename = null;
            Assert.IsFalse(cfg_starter.Validate());

            Assert.ThrowsException<CoreException>(() => { cfg_starter.ValidateAndTrace("starter"); });            
        }

        [TestMethod]
        public void ConvertTest()
        {

            var t = JsonConvert.DeserializeObject<ResolverEntry>("");

            Assert.IsNull(t);
        }



    }
}