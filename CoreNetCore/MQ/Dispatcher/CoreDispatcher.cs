using CoreNetCore.Configuration;
using CoreNetCore.Models;
using CoreNetCore.Utils;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

namespace CoreNetCore.MQ
{
    public class CoreDispatcher : ICoreDispatcher
    {
        private ConcurrentDictionary<string, List<Action<MessageEntry>>> queryHandlers = new ConcurrentDictionary<string, List<Action<MessageEntry>>>();

        private ConcurrentDictionary<string, List<Action<MessageEntry, string>>> responceHandlers = new ConcurrentDictionary<string, List<Action<MessageEntry, string>>>();

        //CallbackMessageEventArgs
        private ConcurrentDictionary<string, Action<string>> responceCallbacks = new ConcurrentDictionary<string, Action<string>>();

        public string SelfServiceName => $"{Config.Starter._this._namespace}:{Config.Starter._this.servicename}:{Config.Starter._this.majorversion}";
        public string ExchangeConnectionString { get; private set; }

        public string QueueConnectionString { get; private set; }

        public IPrepareConfigService Config { get; }

        public string AppId { get; }
        public ICoreConnection Connection { get; }
        public IResolver Resolver { get; }

        private bool running = false;

        public event Action<string> Started;

        public event Action<ReceivedMessageEventArgs, Exception> HandleMessageErrors;

        public CoreDispatcher(IPrepareConfigService config, IAppId appId, ICoreConnection coreConnection, IResolver resolver, IHealthcheck healthcheck)
        {
            Config = config;
            AppId = appId.CurrentUID;
            Connection = coreConnection;
            Resolver = resolver;
            healthcheck.AddCheck(() => running);
            Resolver.Stopped += (appid) => { running = false; };
            Resolver.Started += Resolver_Started;
        }

        private void Resolver_Started(string obj)
        {
            Trace.TraceInformation("Resolver started");

            //Self resolving timeout, exiting ...
            int timeout = Config.Starter.registerselftimeout ?? 10000;
            var register = Resolver.RegisterSelf();
            if (Task.WhenAny(register, Task.Delay(timeout)).Result != register)
            {
                throw new CoreException("Resolver self register timeout expired!");
            }

            var links = register.Result;
            Trace.TraceInformation("Self links resolved");

            int queueInc = 2;
            if (links != null)
            {
                foreach (var link in links)
                {
                    var exchangeName = ExchangeTypes.GetExchangeName(link.name, link.type, null);

                    Trace.TraceInformation($"Bind {exchangeName} start..");

                    ConsumerParam options = new ConsumerParam();
                    options.QueueParam = new ChannelQueueParam()
                    {
                        AutoDelete = Config.MQ.autodelete ?? false,
                    };
                    if (Config.MQ.queue.ttl.HasValue)
                    {
                        options.QueueParam.SetExpiresMs(Config.MQ.queue.ttl.Value);
                    }
                    switch (link.type)
                    {
                        case LinkTypes.LINK_EXCHANGE:
                            {
                                ExchangeConnectionString = exchangeName;
                                options.QueueParam.Name = $"{exchangeName}.{AppId}";
                                options.ExchangeParam = new ChannelExchangeParam()
                                {
                                    Name = exchangeName,
                                    AutoDelete = Config.MQ.autodelete ?? false
                                };
                                if (Config.MQ.exchange.ttl.HasValue)
                                {
                                    options.ExchangeParam.SetExpiresMs(Config.MQ.exchange.ttl.Value);
                                }
                            }
                            break;

                        case LinkTypes.LINK_QUEUE:
                            {
                                QueueConnectionString = exchangeName;
                                options.QueueParam.Name = exchangeName;
                            }
                            break;

                        default: { Trace.TraceWarning($"Unknow link type: [{link.type}]"); continue; }
                    }

                    //Максимальный пул одновременных сообщений контролируется параметром ru:spinosa:mq:prefetch
                    //т.е. максимальным числом полученных сообщений оставленных без ответа
                    Connection.Listen(options, (ea) =>
                    {
                        Task.Run(() => this.HandleMessage(ea))
                        .ContinueWith(res => { this.HandleMessageErrors?.Invoke(ea, res.Exception); }, TaskContinuationOptions.OnlyOnFaulted);
                    });

                    Trace.TraceInformation($"Bind {exchangeName} successed");
                    queueInc--;
                }
            }

            if (queueInc == 0)
            {
                this.running = true;
                //запускаем обновление кэша только если удалось получить собственные ссылки т.к. это подтверждает, что оператор запущен и имеет смысл к нему обращаться
                //

                if (Config.Starter.pingperiod_ms.HasValue)
                {
                    Trace.TraceInformation($"RunRefreshCache started {Config.Starter.pingperiod_ms.Value}ms");
                    Resolver.RunRefreshCache();
                }
                Started?.Invoke(AppId);
            }
            else
            {
                throw new CoreException("Self links bind error:  queueInc!=0 ");
            }
        }

        private void HandleMessage(ReceivedMessageEventArgs ea)
        {
            MessageEntry currentMsg = null;
            try
            {
                if (ea == null)
                {
                    Trace.TraceWarning("Dispatcher HandleMessage: ReceivedMessageEventArgs is null");
                    return;
                }
                currentMsg = new MessageEntry(this, ea);
                if (currentMsg.IsRequest)
                {
                    var methodName = ea.Properties?.ContentType;
                    List<Action<MessageEntry>> handlers;
                    if (!string.IsNullOrEmpty(methodName) && queryHandlers.TryGetValue(methodName, out handlers))
                    {
                        foreach (var action in handlers)
                        {
                            action(currentMsg);
                        }
                    }
                    else
                    {
                        throw new CoreException($"Handler [{methodName}] not declared for this service");
                    }
                }
                else
                {
                    var last_via = currentMsg._via.GetLast();
                    if (last_via != null)
                    {
                        //callbacks
                        if (!string.IsNullOrEmpty(ea.Properties?.MessageId))
                        {
                            Action<string> callback;
                            if (responceCallbacks.TryRemove(ea.Properties.MessageId, out callback) && callback != null)
                            {
                                var data = Encoding.UTF8.GetString(ea.Content);
                                callback(data);
                            }
                        }
                        //resp handlers
                        if (!string.IsNullOrEmpty(last_via.responseHandlerName))
                        {
                            List<Action<MessageEntry, string>> handlers;
                            if (responceHandlers.TryGetValue(last_via.responseHandlerName, out handlers) && handlers != null)
                            {
                                foreach (var action in handlers)
                                {
                                    action(currentMsg, last_via.responseHandlerData?.ToString());
                                }
                            }
                        }
                    }
                    ea.Ack();
                }
            }
            catch (Exception ex)
            {
                if (currentMsg.IsRequest && currentMsg.IsViaValidForResponse())
                {
                    currentMsg.ResponseError(ex);
                }
                throw ex;
            }
        }

        public bool DeclareQueryHandler(string actionName, Action<MessageEntry> handler)
        {
            if (handler == null)
            {
                throw new CoreException("QueryHandler is null");
            }
            Trace.TraceInformation($"Declare query handler. Name={actionName}");
            List<Action<MessageEntry>> handlers = null;
            if (queryHandlers.TryGetValue(actionName, out handlers))
            {
                handlers.Add(handler);
                return true;
            }
            return queryHandlers.TryAdd(actionName, new List<Action<MessageEntry>> { handler });
        }

        public bool DeclareResponseHandler(string actionName, Action<MessageEntry, string> handler)
        {
            if (handler == null)
            {
                throw new CoreException("ResponseHandler is null");
            }
            Trace.TraceInformation($"Declare response handler. Name={actionName}");
            List<Action<MessageEntry, string>> handlers = null;
            if (responceHandlers.TryGetValue(actionName, out handlers))
            {
                handlers.Add(handler);
                return true;
            }
            return responceHandlers.TryAdd(actionName, new List<Action<MessageEntry, string>> { handler });
        }

        public bool DeclareResponseCallback(string messageId, Action<string> callback, int? timeout)
        {
            bool success = true;
            try
            {
                if (callback == null)
                {
                    throw new CoreException("Callback is null");
                }
                Trace.TraceInformation($"Declare response callback. MessageId={messageId}");
                responceCallbacks.AddOrUpdate(messageId, callback, (k, v) => callback);
                return true;
            }
            catch (Exception ex)
            {
                success = false;
                throw ex;
            }
            finally
            {
                if (success && timeout.HasValue)
                {
                    Task.Delay(timeout.Value).ContinueWith(res =>
                    {
                        Action<string> cb;
                        if (responceCallbacks.TryRemove(messageId, out cb) && cb != null)
                        {
                            var errContext = new DataArgs<object>(new CoreException($"Callback timeout expired. MessageId=[{messageId}]"));
                            callback(errContext.ToJson());
                        }
                    });
                }
            }
        }

        public string GetConnectionByExchangeType(string exchangeKind)
        {
            return LinkTypes.GetLinkByExchangeType(exchangeKind) == LinkTypes.LINK_EXCHANGE ? ExchangeConnectionString : QueueConnectionString;
        }

        public Task<string> Resolve(string service, string type)
        {
            return Resolver.Resolve(service, type);
        }
    }
}