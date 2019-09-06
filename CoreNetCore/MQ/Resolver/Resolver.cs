using CoreNetCore.Configuration;
using CoreNetCore.Models;
using CoreNetCore.Utils;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CoreNetCore.MQ
{
    public class Resolver : IResolver
    {
        private IMemoryCache Cache { get; }
        private ConcurrentBag<string> linkCacheKeys = new ConcurrentBag<string>();
        public const string SELF_LINK_SIGN = "self_link";

        private ConcurrentDictionary<string, PendingEventArgs> pending = new ConcurrentDictionary<string, PendingEventArgs>();
        private CancellationTokenSource cancellationRefreshCache;

        public bool Bind { get; private set; }
        private ICoreConnection Connection { get; }
        private CfgStarterSection cfg_starter => Configuration?.Starter;

        public IPrepareConfigService Configuration { get; }
        public string AppId { get; }

        public event Action<string> Started;

        public event Action<string> Stopped;

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
            connection.Disconnected += (appid) =>
            {
                Bind = false;
                Stopped?.Invoke(appid);
                if (cancellationRefreshCache != null)
                {
                    Trace.TraceInformation("Cache refresh stopped");
                    cancellationRefreshCache.Cancel();
                }
            };
        }

        private void Connection_Connected(string appId)
        {
            var refreshInterval = cfg_starter.pingperiod_ms;

            var cparam = new ConsumerParam()
            {
                QueueParam = new ChannelQueueParam()
                {
                    Name = $"{cfg_starter._this._namespace}.{cfg_starter._this.servicename}.resolver.{appId}",
                    AutoDelete = true,
                    Durable = true
                },
                ExchangeParam = new ChannelExchangeParam()
                {
                    Name = cfg_starter.responseexchangename,
                    Type = ExchangeTypes.EXCHANGETYPE_DIRECT,
                    AutoDelete = false,
                    Durable = true
                }
            };

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
                         var key = GetKey(element.Namespace, element.service, element.version ?? 0);
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
                                     AddToLinkCache(key, cache_item);
                                 }
                                 //отвечаем на запросы
                                 var tsc = GetPengingContext(key);
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
                             var tsc = GetPengingContext(key);
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

            //Cache Refresh
            cancellationRefreshCache = new CancellationTokenSource();
            if (Configuration.Starter.pingperiod_ms.HasValue) {
                RefreshCache(Configuration.Starter.pingperiod_ms.Value, cancellationRefreshCache.Token);
            }

            Bind = true;
            //SendToOperator(true).Wait();
            //SendToOperator(false).Wait();

            Started?.Invoke(AppId);
            Trace.TraceInformation("Resolver queue created");
        }

        private void AddToLinkCache(string key, CacheItem cache_item)
        {
            Cache.Set(key, cache_item, GetCacheOptions());

            lock (linkCacheKeys) {
                if (!linkCacheKeys.Contains(key))
                {
                    linkCacheKeys.Add(key);
                }
            }
        }

        private async Task RefreshCache(int timeout, CancellationToken cancelTokenRefreshCache)
        {
            await Task.Delay(timeout, cancelTokenRefreshCache);

            if (cancelTokenRefreshCache.IsCancellationRequested) return;
            if (this.Bind)
            {
                Trace.TraceInformation("RefreshCache");

                foreach (var key in linkCacheKeys)
                {
                    var cacheItem = Cache.Get<CacheItem>(key);
                    if (cacheItem != null)
                    {
                        TaskCompletionSource<LinkEntry[]> context = null;
                        if (cancelTokenRefreshCache.IsCancellationRequested) return;
                        GetOrGeneratePendingContext(cacheItem.Namespace, cacheItem.service, cacheItem.version ?? 1, cacheItem.sub_version, false, out context, true);
                    }
                }
                if (cancelTokenRefreshCache.IsCancellationRequested) return;
                await SendToOperator(false);
            }

            RefreshCache(timeout, cancelTokenRefreshCache);
        }

        public async Task<string> Resolve(string service, string type)
        {
            //var nsv = Regex.Split(service, @"/([A-Za-z0-9-\._]{1,100}?):([A-Za-z0-9:\.-_]{1,100}?):([0-9]{1,9}?)/");
            var nsv = service?.Split(':');
            if (nsv == null || nsv.Length < 3)
            {
                throw new CoreException("Resolver: could not parse service name");
            }

            var vers = 0;
            if (!int.TryParse(nsv[2], out vers))
            {
                Trace.TraceWarning($"Service version not parse. Service: {service}");
            }
            //await caching query or pending
            var links = await GetLinks(nsv[0], nsv[1], vers, false);
            //return query name by type kind
            return links?.Where(x => x.type?.Equals(type, StringComparison.InvariantCultureIgnoreCase) ?? false).Select(x => x.name).FirstOrDefault();
        }

        public Task<LinkEntry[]> RegisterSelf()
        {
            Trace.TraceInformation("Register Self started..");
            string name_space = cfg_starter._this._namespace;
            string service = cfg_starter._this.servicename;
            int version = cfg_starter._this.majorversion ?? 0;
            string sub_ver = cfg_starter._this.subversion;

            TaskCompletionSource<LinkEntry[]> context;
            if (!GetOrGeneratePendingContext(name_space, service, version, sub_ver, true, out context))
            {
                //если объекта ожидания еще небыло в очереди - посылаем на запуск очереди ожидания
                SendToOperator(true)
                    .ContinueWith(res =>
                {
                    if (res.Exception != null)
                    {
                        context.SetException(res.Exception);
                    }
                });
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
                if (!GetOrGeneratePendingContext(name_space, service, version, null, isSelf, out context))
                {
                    //если объекта ожидания еще небыло в очереди - посылаем на запуск очереди ожидания
                    SendToOperator(isSelf)
                        .ContinueWith(res =>
                        {
                            if (res.Exception != null)
                            {
                                context.SetException(res.Exception);
                            }
                        });
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
        private Task Send(string exchange, bool isSelf)
        {
            return Task.Run(() =>
            {
                if (this.Bind)
                {
                    List<object> request = new List<object>();
                    List<TaskCompletionSource<LinkEntry[]>> cancelledTacks = new List<TaskCompletionSource<LinkEntry[]>>();
                    foreach (var p_item in pending)
                    {
                        var pendingElement = p_item.Value;
                        //если объект соответствует типу и еще не запущен
                        if (pendingElement != null && pendingElement.IsSelf == isSelf && !pendingElement.IsSend)
                        {
                            request.Add(pendingElement.Request);
                            cancelledTacks.Add(pendingElement.Context);
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
                                
                                //Trace.TraceInformation("Resolver publish->" + data_str);
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
            });
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
        private bool GetOrGeneratePendingContext(string name_space, string service, int version, string sub_version, bool isSelf, out TaskCompletionSource<LinkEntry[]> context, bool isCacheUpdate = false)
        {
            lock (pending)
            {
                var serviceKey = GetKey(name_space, service, version);

                PendingEventArgs pElement;
                if (pending.TryGetValue(serviceKey, out pElement) && pElement != null)
                {
                    context = pElement.Context;
                    return true;
                }

                context = isCacheUpdate ? null : new TaskCompletionSource<LinkEntry[]>();

                var pendingElement = new PendingEventArgs()
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
                    },
                    Context = context
                };

                pending.TryAdd(serviceKey, pendingElement);
                return false;
            }
        }

        private string GetKey(string name_space, string service, int version) => $"{name_space}:{service}:{version}";

        private MemoryCacheEntryOptions GetCacheOptions()
        {
            return new MemoryCacheEntryOptions()
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(cfg_starter.cacheEntryTTL_sec ?? 0)
            };
        }

        private TaskCompletionSource<LinkEntry[]> GetPengingContext(string key)
        {
            PendingEventArgs pendingElement;
            if (!this.pending.TryGetValue(key, out pendingElement) || pendingElement == null)
            {
                Trace.TraceWarning($"Resolver: get pending element fail. Key:{key}");
                return null;
            }
            return pendingElement.Context;
        }

        private bool RemovePengingElement(string key)
        {
            PendingEventArgs pendingElement;
            if (this.pending.TryRemove(key, out pendingElement))
            {
                return true;
            }
            Trace.TraceWarning($"Resolver: remove pending element fail. Key:{key}");
            return false;
        }
    }
}