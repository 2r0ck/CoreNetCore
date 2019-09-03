using CoreNetCore.Configuration;
using CoreNetCore.Models;
using CoreNetCore.Utils;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CoreNetCore.MQ
{
    public class Resolver : IResolver
    {
        private IMemoryCache Cache { get; }
        public const string SELF_LINK_SIGN = "self_link";

        private ConcurrentDictionary<string, TaskCompletionSource<LinkEntry[]>> pending = new ConcurrentDictionary<string, TaskCompletionSource<LinkEntry[]>>();

        public bool Bind { get; private set; }
        private ICoreConnection Connection { get; }
        private CfgStarterSection cfg_starter => Configuration?.Starter;

        public IPrepareConfigService Configuration { get; }
        public string AppId { get; }

        // public ConcurrentDictionary<string, List<ResolverInvoker>> pending = new ConcurrentDictionary<string, List<ResolverInvoker>>();

        public Resolver(IMemoryCache memoryCache, ICoreConnection connection, IPrepareConfigService configuration, IHealthcheck healthcheck, IAppId appId)
        {
            Configuration = configuration;
            AppId = appId.CurrentUID;
            Connection = connection;

            Cache = memoryCache;
            Bind = false;
            healthcheck.AddCheck(() => Bind);
            connection.Connected += Connection_Connected;
        }


        private void Connection_Connected(string appId)
        {
           
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
            //TODO: refresh interval!!!!
            Connection.Listen(cparam, (msg) =>
            {
                //очередь принимает массив ссылок (links)
                var responceStr = System.Text.Encoding.UTF8.GetString(msg.Content);
                var responce = responceStr.FromJson<List<ResolverEntry>>();

                if (responce?.Any() ?? false)
                {
                    //по каждой ссылке
                    foreach (var element in responce)
                    {
                        //ключ -  имя сервиса с версией
                        var key = GetKey(element.Namespace, element.service, element.version??0);
                        try
                        {
                            if (element.result)
                            {
                                var links = element?.link;

                                if (links == null)
                                {
                                    throw new CoreException($"Resolver queue: Empty links for service. Key: {key}");
                                }
                                //если не self очереди - кэшируем
                                if (element.type != CacheItem.SELF_TOKEN)
                                {
                                    //создаем элемент в кэше                                
                                    var cache_item = new CacheItem()
                                    {
                                        Namespace = element.Namespace,
                                        service = element.service,
                                        version = element.version,
                                        sub_version = element.sub_version,
                                        self_link = false,
                                        links = links
                                    };
                                    Cache.Set(key, cache_item, GetCacheOptions());
                                }
                                //отвечаем на запросы
                                var tsc = GetPengingValue(key);
                                if (tsc != null)
                                {
                                    tsc.SetResult(links.ToArray());
                                }
                            }
                            else
                            {
                                throw new CoreException($"Resolver queue: response element result is false. Key: {key}");
                            }
                        }
                        catch (Exception ex)
                        {
                            Trace.TraceWarning(($"Resolver queue error: {ex}"));
                            var tsc = GetPengingValue(key);
                            tsc?.SetException(ex);
                        }
                        finally
                        {
                            RemovePengingElement(key);
                        }                       
                    }
                }
                msg.Ask();
            });
            Trace.TraceInformation("Resolver queue created");
            Bind = true;
        }



        public  async Task<string> Resolve(string service, string type)
        {
            var nsv = Regex.Split(service, @"/([A-Za-z0-9-\._]{1,100}?):([A-Za-z0-9:\.-_]{1,100}?):([0-9]{1,9}?)/");
            if (nsv == null || nsv.Length < 3)
            {
                throw new CoreException("Resolver: could not parse service name");
            }

            var vers = 0;
            if(!int.TryParse(nsv[2], out vers))
            {
                Trace.TraceWarning($"Service version not parse. Service: {service}");
            }
            //await caching query or pending
            var links = await GetLinks(nsv[0], nsv[1], vers, false);
            //return query name by type kind
            return links?.Where(x => x.type?.Equals(type, StringComparison.InvariantCultureIgnoreCase) ?? false).Select(x => x.name).FirstOrDefault();
        }

        private Task<LinkEntry[]> RegisterSelf()
        {
            Trace.TraceInformation("Register Self started..");
            string name_space = cfg_starter._this._namespace;
            string service = cfg_starter._this.servicename;
            int version = cfg_starter._this.majorversion?? 0;
            string sub_ver = cfg_starter._this.subversion;

            TaskCompletionSource<LinkEntry[]> context;
            if (GetOrGeneratePendingContext(name_space, service, version, sub_ver, true, out context))
            {
                //если объекта ожидания еще небыло в очереди - посылаем на запуск очереди ожидания
                SendToOperator(true);
            }
            return context.Task;

        }

        private Task<LinkEntry[]> GetLinks(string name_space, string service, int version, bool isSelf)
        {
            var serviceKey = GetKey(name_space, service, version);
            var cacheEntry = isSelf ? null : Cache.Get<CacheItem>(serviceKey);
            if (cacheEntry == null)
            {
                TaskCompletionSource<LinkEntry[]> context;
                if (!GetOrGeneratePendingContext(name_space, service, version,null, isSelf, out context))
                {
                    //если объекта ожидания еще небыло в очереди - посылаем на запуск очереди ожидания
                    SendToOperator(isSelf);
                }
                return context.Task;
            }
            else
            {
                return Task.Run(() => cacheEntry.links);
            }
        }

        //namespace:service:version

        /// <summary>
        /// Опрос оператора. 
        /// Либо в очередь диспатчера - для оповещении о себе и получении информации на каких очередях подниматься,
        /// либо в очередь операций для информации об очередях окружающих сервисов
        /// </summary>
        /// <param name="isSelf"></param>
        /// <returns></returns>
        private async Task SendToOperator(bool isSelf)
        {
            if (isSelf)
            {
                //input.dispatcher.core
                await Send(cfg_starter.requestdispatcherexchangename, true);
            }
            else
            {
                //input.operator.core
                await Send(cfg_starter.requestexchangename, false);
            }
        }

        /// <summary>
        /// Опрос оператора
        /// </summary>
        /// <param name="exchange"></param>
        /// <param name="isSelf"></param>
        /// <returns></returns>
        private async Task Send(string exchange, bool isSelf)
        {
            if (this.Bind)
            {
                List<object> request = new List<object>();
                List<TaskCompletionSource<LinkEntry[]>> cancelledTacks = new List<TaskCompletionSource<LinkEntry[]>>();
                foreach (var p_item in pending)
                {
                    var state = p_item.Value?.Task?.AsyncState as PendingEventArgs;
                    //если объект соответствует типу и еще не запущен
                    if (state != null && state.IsSelf == isSelf && !state.IsSend)
                    {
                        request.Add(state.Request);
                        cancelledTacks.Add(p_item.Value);
                    }
                }
                if (request.Any())
                {
                    try
                    {
                        var data_str = request.ToJson(true);
                        if (data_str != null)
                        {
                            var properties = Connection.CreateChannelProperties();
                            properties.CorrelationId = Guid.NewGuid().ToString();
                            properties.ReplyTo = cfg_starter.responseexchangename;
                            properties.AppId = AppId;
                            properties.MessageId = Guid.NewGuid().ToString();

                            ProducerParam options = new ProducerParam()
                            {
                                ExchangeParam = new ChannelExchangeParam()
                                {
                                    Name = exchange,
                                    Type = ExchangeTypes.EXCHANGETYPE_FANOUT
                                }
                            };
                            //отправляем скопом всех ожидающих процессов в одном запросе
                            Connection.Publish(options, Encoding.UTF8.GetBytes(data_str), properties);
                        }
                    }
                    catch (Exception exception)
                    {
                        foreach (var taskcs in cancelledTacks)
                        {
                            taskcs.SetException(exception);
                        }
                    }
                }
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="key"></param>
        /// <param name="context"></param>
        /// <returns>
        /// Возвращает true если объект уже был в очереди ожидания
        /// </returns>
        /// <remarks>
        /// lock необходим для того, чтобы операция добавления и возврата значений была атомарна
        /// отдельный метод необходим для  того, чтобы знать добавляется или возвращается элемент
        /// </remarks>
        private bool GetOrGeneratePendingContext(string name_space, string service, int version, string sub_version, bool isSelf, out TaskCompletionSource<LinkEntry[]> context)
        {
            lock (pending)
            {
                var serviceKey = GetKey(name_space, service, version);
 

                if (pending.TryGetValue(serviceKey, out context))
                {
                    return true;
                }

                context = new TaskCompletionSource<LinkEntry[]>(new PendingEventArgs()
                {
                    IsSelf = isSelf,
                    IsSend = false,
                    Request = new ResolverEntry
                    {
                        Namespace = name_space,
                        service = service,
                        version = version,
                        sub_version = sub_version,
                        type = isSelf ? SELF_LINK_SIGN : null
                    }
                });
                pending.TryAdd(serviceKey, context);
                return false;
            }
        }

        private string GetKey(string name_space, string service, int version) => $"{name_space}:{service}:{version}";


        private MemoryCacheEntryOptions GetCacheOptions()
        {            
            return   new MemoryCacheEntryOptions()
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(cfg_starter.cacheEntryTTL_sec ?? 0)
            };
        }

        private TaskCompletionSource<LinkEntry[]> GetPengingValue(string key)
        {
            TaskCompletionSource<LinkEntry[]> value = null;
            if (!this.pending.TryGetValue(key,out value))
            {
                Trace.TraceWarning($"Resolver: get pending element fail. Key:{key}");
            }
            return value;
        }

        private bool RemovePengingElement(string key)
        {
            TaskCompletionSource<LinkEntry[]> value = null;
            if (this.pending.TryRemove(key, out value))
            {                
                return true;
            }
            Trace.TraceWarning($"Resolver: remove pending element fail. Key:{key}");
            return false;
        }
    }
}