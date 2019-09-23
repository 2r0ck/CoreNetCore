using CoreNetCore;
using CoreNetCore.Configuration;
using CoreNetCore.MQ;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Diagnostics;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace CoreNetCoreTest.MQ
{
    [TestClass]
    public class HealthcheckTest
    {


        [TestMethod]
        public async Task HealthcheckTesting1()
        {
            var hostBuilder = new CoreHostBuilder();
            var host = hostBuilder.Build();

            var hs = host.Services.GetService<IHealthcheck>();
#pragma warning disable CS4014 // Так как этот вызов не ожидается, выполнение существующего метода продолжается до завершения вызова
            hs.StartAsync();
#pragma warning restore CS4014 // Так как этот вызов не ожидается, выполнение существующего метода продолжается до завершения вызова
            var configService = host.Services.GetService<IPrepareConfigService>();          

            string healthcheckUrl = $"Http://localhost:{configService.MQ.healthcheckPort}/healthcheck";

            Thread.Sleep(1000);

            Assert.AreEqual(await CheckAnwer(healthcheckUrl), true);

            hs.AddCheck(() => true);
            Assert.AreEqual(await CheckAnwer(healthcheckUrl), true);

            hs.AddCheck(() => false);
            Assert.AreEqual(await CheckAnwer(healthcheckUrl), false);

            hs.AddCheck(() => true);
            Assert.AreEqual(await CheckAnwer(healthcheckUrl), false);

            hs.Stop();
        }


        [TestMethod]
        public async Task HealthcheckTesting2()
        {
            var hostBuilder = new CoreHostBuilder();
            var host = hostBuilder.Build();

            var hs = host.Services.GetService<IHealthcheck>();
#pragma warning disable CS4014 // Так как этот вызов не ожидается, выполнение существующего метода продолжается до завершения вызова
            hs.StartAsync();
#pragma warning restore CS4014 // Так как этот вызов не ожидается, выполнение существующего метода продолжается до завершения вызова

            var configService = host.Services.GetService<IPrepareConfigService>();

            string healthcheckUrl = $"Http://localhost:{configService.MQ.healthcheckPort}/healthcheck";
            Thread.Sleep(1000);

            int a = 0;

            Func<bool> handler = () =>
            {
                return a < 10;
            };

            hs.AddCheck(handler);

            Assert.AreEqual(await CheckAnwer(healthcheckUrl), true);

            a = 6;
            Assert.AreEqual(await CheckAnwer(healthcheckUrl), true);

            a = 13;
            Assert.AreEqual(await CheckAnwer(healthcheckUrl), false);

            a = 0;
            Assert.AreEqual(await CheckAnwer(healthcheckUrl), true);

            hs.Stop();
        }

        private async Task<bool>  CheckAnwer(string url)
        {
            using (HttpClient client = new HttpClient())
            {
                //https://rancher.com/docs/rancher/v1.3/en/cattle/health-checks/                

                var resp = await client.GetAsync(url);
                Trace.TraceInformation($"Url: [{url}]. StatusCode={resp.StatusCode}({(int)resp.StatusCode})");
                //HTTP Responds 2xx / 3xx,
                return ((int)resp.StatusCode >= 200) && ((int)resp.StatusCode <= 399);

            }
        }
    }
}