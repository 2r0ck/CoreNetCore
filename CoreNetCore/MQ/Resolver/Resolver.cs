using CoreNetCore.Configuration;
using CoreNetCore.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;

namespace CoreNetCore.MQ
{
    public class Resolver
    {
        private IMemoryCache Cache { get; }

        public bool Bind { get; private set; }
        private ICoreConnection Connection { get; }
        private CfgStarterSection cfg_starter;

        public Resolver(IMemoryCache memoryCache, ICoreConnection connection, IConfiguration configuration, IHealthcheck healthcheck)
        {
            Connection = connection;
            ReadConfig(configuration);
            Cache = memoryCache;
            Bind = false;
            healthcheck.AddCheck(() => Bind);

            connection.Connected += Connection_Connected;
        }

        private void Connection_Connected(string appId)
        {
            var ttl = cfg_starter.cacheEntryTTL_sec;
            var refreshInterval = cfg_starter.pingperiod_ms;
        
            var cparam = new ConsumerParam()
            {
                QueueParam = new ChannelQueueParam()
                {
                    Name = $"{cfg_starter._this._namespace}.{cfg_starter._this.servicename}.resolver.{appId}",
                    AutoDelete = true
                },
                ExchangeParam = new ChannelExchangeParam()
                {
                    Name = cfg_starter.responseexchangename,
                    Type = ExchangeTypes.EXCHANGETYPE_DIRECT,
                    AutoDelete = true
                }
            };
            Connection.Listen(cparam, (msg) =>
            {
                var responceStr = System.Text.Encoding.UTF8.GetString(msg.Content);
                var responce = JsonConvert.DeserializeObject<List<ResolverEntry>>(responceStr);
                if (responce?.Any()??false)
                {
                    foreach (var element in responce)
                    {
                        var key = $"{element.Namespace}:{element.service}:{element.version}";
                        if (element.result)
                        {
                            var cache_item = new CacheItem()
                            {
                                Namespace = element.Namespace,
                                service = element.service,
                                version = element.version,
                                sub_version = element.sub_version,
                                self_link = element.type == CacheItem.SELF_TOKEN
                            };
                            if (element.link != null)
                            {
                                foreach (var item in element.link)
                                {
                                    cache_item.AddLink(item.type, item.name);
                                }
                            }

                            if (!cache_item.self_link)
                            {

                            }                            
                        }

                    }
                }
            });
        }

        private void ReadConfig(IConfiguration configuration)
        {
            cfg_starter = new CfgStarterSection();
            configuration.GetSection("starter").Bind(cfg_starter, options => options.BindNonPublicProperties = true);
            cfg_starter.ValidateAndTrace("starter");
        }


    }
}