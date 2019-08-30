using CoreNetCore.Configuration;
using CoreNetCore.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace CoreNetCore.MQ
{
    public class Resolver : IResolver
    {
        private IMemoryCache Cache { get; }

        public bool Bind { get; private set; }
        private ICoreConnection Connection { get; }
        private CfgStarterSection cfg_starter = null; //TODO: from service!!!!!!!!!!!!!!!!!!!!!!!!!!!

        public ConcurrentDictionary<string, List<ResolverInvoker>> pending = new ConcurrentDictionary<string, List<ResolverInvoker>>();

        public Resolver(IMemoryCache memoryCache, ICoreConnection connection, IConfiguration configuration, IHealthcheck healthcheck)
        {
            Connection = connection;
            
            Cache = memoryCache;
            Bind = false;
            healthcheck.AddCheck(() => Bind);

            connection.Connected += Connection_Connected;
        }

        private void Connection_Connected(string appId)
        {
            var ttl = cfg_starter.cacheEntryTTL_sec ?? 0;
            var refreshInterval = cfg_starter.pingperiod_ms;
            var cacheOption = new MemoryCacheEntryOptions()
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(ttl)
            };
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
                //очередь принимает массив резольверов
                var responceStr = System.Text.Encoding.UTF8.GetString(msg.Content);
                var responce = JsonConvert.DeserializeObject<List<ResolverEntry>>(responceStr);

                if (responce?.Any() ?? false)
                {
                    //для каждого резольвера
                    foreach (var element in responce)
                    {
                        //ключ -  имя сервиса с версией
                        var key = $"{element.Namespace}:{element.service}:{element.version}";

                        if (element.result)
                        {
                            //создаем элемент в кэше
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

                            //если запрос не от самого резольвера - кэшируем
                            if (!cache_item.self_link)
                            {
                                Cache.Set(key, cache_item, cacheOption);
                            }
                            //запускаем
                            ExecutePendingElement(key, cache_item.links);
                        }
                        else
                        {                            
                            FailedPendingElement(key, element.error);
                        }

                        DeletePendingElement(key);                        
                    }
                }

                msg.Ask();
            });
            Trace.TraceInformation("Resolver queue created");
            Bind = true;
            
        }

        private List<ResolverInvoker> DeletePendingElement(string key)
        {
            List<ResolverInvoker> invokers = null;
            if (pending.TryRemove(key, out invokers))
            {
                return invokers;
            }
            return null;
        }

        private void ExecutePendingElement(string key, Dictionary<string, string> links)
        {
            List<ResolverInvoker> invokers = null;
            if (pending.TryGetValue(key, out invokers))
            {
                foreach (var inv in invokers)
                {
                    var res = inv.Progress(links);
                    if (res == null)
                    {
                        inv.FailCallback?.Invoke($"not available for service {key}");
                    }
                    else
                    {
                        inv.SuccessCallback?.Invoke(res);
                    }
                }
            }
        }

        private void FailedPendingElement(string key, string error_message)
        {
            List<ResolverInvoker> invokers = null;
            if (pending.TryGetValue(key, out invokers))
            {
                foreach (var inv in invokers)
                {
                    inv.FailCallback?.Invoke(error_message);
                }
            }
        }

        public string Resolve(string service, string type) {

            return "";

        }


    }
}