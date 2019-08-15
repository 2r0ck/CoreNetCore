using CoreNetCore.Models;
using CoreNetCore.Utils;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Diagnostics;

namespace CoreNetCoreTest.Utils
{
    [TestClass]
    public class ExtensionsTest
    {
        [TestMethod]
        public void JsonConvertTest()
        {
            var r = new ChannelQueueParam(name: "TestQueueName",
                arguments: new Dictionary<string, object> {
                { "key1","value1" },{ "key2",2 },{ "key3",true }
            });

            var json = r.ToJson();

            Assert.IsNotNull(json);
            Assert.AreNotEqual(json, "[Object to Json serialize error]");

            var t = Newtonsoft.Json.JsonConvert.DeserializeObject<ChannelQueueParam>(json);

            Assert.AreEqual(t.Name, r.Name);

            Assert.AreEqual(int.Parse(t.Arguments["key2"].ToString()), int.Parse(r.Arguments["key2"].ToString()));

            Trace.WriteLine(json);
        }
    }
}