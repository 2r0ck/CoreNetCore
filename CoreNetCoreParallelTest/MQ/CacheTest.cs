using CoreNetCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading;

namespace CoreNetCoreParallelTest.MQ
{
    [TestClass]
    public class CacheTest
    {
        [TestMethod]
        public void ChacheTest1()
        {
            var host = new CoreHostBuilder().Build();
            var cacheService = host.Services.GetService<IMemoryCache>();

            var value1 = "value";
            var value2 = new {t=1,msg="hello"};
            var value3 = 13;
            var value4 = 0.5m;

            cacheService.Set("key1", value1);
            cacheService.Set("key2", value2);

            Assert.AreEqual(cacheService.Get<string>("key1"), value1);

            Assert.AreEqual(cacheService.Get<object>("key2"), value2);

            cacheService.Set("key3", value3, new MemoryCacheEntryOptions() {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(1)                
            });
            Assert.AreEqual(cacheService.Get<object>("key3"), value3);
            Thread.Sleep(1200);
            Assert.IsNull(cacheService.Get<object>("key3"));


            cacheService.Set("key4", value4, new MemoryCacheEntryOptions()
            {
                SlidingExpiration = TimeSpan.FromSeconds(2),                
            });
            Assert.AreEqual(cacheService.Get<object>("key4"), value4);
            Thread.Sleep(1000);
            Assert.AreEqual(cacheService.Get<object>("key4"), value4);
            Thread.Sleep(1000);
            Assert.AreEqual(cacheService.Get<object>("key4"), value4);
            Thread.Sleep(1000);
            Assert.AreEqual(cacheService.Get<object>("key4"), value4);
            Thread.Sleep(2100);
            Assert.IsNull(cacheService.Get<object>("key4")); 
        }
    }
}