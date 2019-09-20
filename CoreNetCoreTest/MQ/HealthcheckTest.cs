using CoreNetCore;
using CoreNetCore.Configuration;
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
            var configService = host.Services.GetService<IPrepareConfigService>();          

            string healthcheckUrl = $"Http://localhost:{configService.MQ.healthcheckPort}/healthcheck";

            Thread.Sleep(1000);

            CheckAnwer(healthcheckUrl, "True");

            hs.AddCheck(() => true);
            CheckAnwer(healthcheckUrl, "True");

            hs.AddCheck(() => false);
            CheckAnwer(healthcheckUrl, "False");

            hs.AddCheck(() => true);
            CheckAnwer(healthcheckUrl, "False");

            hs.Stop();
        }


        [TestMethod]
        public void HealthcheckTesting2()
        {
            var hostBuilder = new CoreHostBuilder();
            var host = hostBuilder.Build();

            var hs = host.Services.GetService<IHealthcheck>();
            hs.StartAsync();

            var configService = host.Services.GetService<IPrepareConfigService>();

            string healthcheckUrl = $"Http://localhost:{configService.MQ.healthcheckPort}/healthcheck";
            Thread.Sleep(1000);

            int a = 0;

            Func<bool> handler = () =>
            {
                return a < 10;
            };

            hs.AddCheck(handler);

            CheckAnwer(healthcheckUrl, "True");

            a = 6;
            CheckAnwer(healthcheckUrl, "True");

            a = 13;
            CheckAnwer(healthcheckUrl, "False");

            a = 0;
            CheckAnwer(healthcheckUrl, "True");

            hs.Stop();
        }

        private void CheckAnwer(string url, string estimatedAnswer)
        {
            using (HttpClient client = new HttpClient())
            {
                var query = client.GetStringAsync(url);
                var result = query.Result;
                Assert.AreEqual(result?.Trim(), estimatedAnswer?.Trim(),true);
            }
        }
    }
}