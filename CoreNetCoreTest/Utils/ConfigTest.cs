using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using CoreNetCore;
using CoreNetCore.Utils;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CoreNetCoreTest.Utils
{
    [TestClass]
    public class ConfigTest
    {
       





        [TestMethod]
        public void DefaultConfigFile_Test()
        {
            var factory = ConfigurationFactory.CreateConfiguration();
            var testValue = factory.GetStrValue("defaultConfigKey");
            Assert.AreEqual("test value", testValue);
        }

        [TestMethod]
        public void CustomConfigFiles_Test()
        {
            //Set variables
            var files = Directory.GetFiles(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\Config"));
            Environment.SetEnvironmentVariable(Core.ENVRIOMENT_CONFIG_FILE_NAMES, string.Join(",", files));
                        
            var factory = ConfigurationFactory.CreateConfiguration();

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

            Environment.SetEnvironmentVariable(Core.ENVRIOMENT_CONFIG_APP_PREFIX+ testEnvriomentName, testEnvriomentValue);

            var factory = ConfigurationFactory.CreateConfiguration();

            Assert.AreEqual(factory[testEnvriomentName], testEnvriomentValue);
        }


        [TestMethod]
        public void EnvironmentRewriteConfig_Test()
        {

            var testJSONAndEnvriomentKey = "envCustomTest";
            var testEnvriomentValue = "envCustomTest ENV VALUE";
            var testJSONValue = "EnvCustomTest JSON Value";

            //Set cfg
            var files = Directory.GetFiles(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\Config"));
            Environment.SetEnvironmentVariable(Core.ENVRIOMENT_CONFIG_FILE_NAMES, string.Join(",", files));

            //Set variables
            Environment.SetEnvironmentVariable(Core.ENVRIOMENT_CONFIG_APP_PREFIX + testJSONAndEnvriomentKey, testEnvriomentValue);

            var factory = ConfigurationFactory.CreateConfiguration();
            //rewrite
            Assert.AreEqual(factory[testJSONAndEnvriomentKey], testEnvriomentValue);
        }
    }
}
