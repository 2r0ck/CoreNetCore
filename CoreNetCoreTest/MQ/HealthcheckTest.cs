using CoreNetCore;
using CoreNetCore.MQ;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Net.Http;
using System.Threading;

namespace CoreNetCoreTest.MQ
{
    [TestClass]
    public class HealthcheckTest
    {
        [TestMethod]
        public void HealthcheckTesting1()
        {
            var hostBuilder = new CoreHostBuilder();
            var host = hostBuilder.Build();

            var hs = host.Services.GetService<IHealthcheck>();
            hs.StartAsync();

            Thread.Sleep(1000);

            CheckAnwer("Http://localhost:8048/healthcheck", "True");

            hs.AddCheck(() => true);
            CheckAnwer("Http://localhost:8048/healthcheck", "True");

            hs.AddCheck(() => false);
            CheckAnwer("Http://localhost:8048/healthcheck", "False");

            hs.AddCheck(() => true);
            CheckAnwer("Http://localhost:8048/healthcheck", "False");

            hs.Stop();
        }


        [TestMethod]
        public void HealthcheckTesting2()
        {
            var hostBuilder = new CoreHostBuilder();
            var host = hostBuilder.Build();

            var hs = host.Services.GetService<IHealthcheck>();
            hs.StartAsync();

            Thread.Sleep(1000);

            int a = 0;

            Func<bool> heandler = () =>
            {
                return a < 10;
            };

            hs.AddCheck(heandler);

            CheckAnwer("Http://localhost:8048/healthcheck", "True");

            a = 6;
            CheckAnwer("Http://localhost:8048/healthcheck", "True");

            a = 13;
            CheckAnwer("Http://localhost:8048/healthcheck", "False");

            a = 0;
            CheckAnwer("Http://localhost:8048/healthcheck", "True");

            hs.Stop();
        }

        private void CheckAnwer(string url, string estimatedAnswer)
        {
            using (HttpClient client = new HttpClient())
            {
                var result = client.GetStringAsync(url).Result;
                Assert.AreEqual(result?.Trim(), estimatedAnswer?.Trim(),true);
            }
        }
    }
}