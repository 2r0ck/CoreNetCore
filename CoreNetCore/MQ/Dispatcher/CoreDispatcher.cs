using CoreNetCore.Configuration;
using CoreNetCore.Models;
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
        private ConcurrentDictionary<string, List<Action<string>>> responceCallbacks = new ConcurrentDictionary<string, List<Action<string>>>();

        public string SelfServiceName => $"{Config.Starter._this._namespace}:{Config.Starter._this.servicename}:{Config.Starter._this.majorversion}";
        public string ExchangeConnectionString { get;  private set;}

        public string QueueConnectionString { get; private set; }

        public IPrepareConfigService Config { get; }

        public string AppId { get; }
        public ICoreConnection Connection { get; }
        public IResolver Resolver { get; }

        private bool running = false;

        public event Action<string> Started;

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
            //TODO: Self resolving timeout, exiting ...
            Trace.TraceInformation("Resolver started");

            var links = Resolver.RegisterSelf().Result;
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
                        AutoDelete = Config.MQ.autodelete ?? false
                        //todo: expires (mq.queue.ttl)
                    };
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
                                    //todo: expires (mq.bind.ttl)
                                };
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

                    Connection.Listen(options, (ea) => {
                        this.HandleMessage(ea);
                    });

                    Trace.TraceInformation($"Bind {exchangeName} successed");
                    queueInc--;
                }
            }

            if (queueInc == 0)
            {
                this.running = true;
                Started?.Invoke(AppId);
            }
            else
            {
                throw new CoreException("Self links bind error:  queueInc!=0 ");
            }
        }

        private void HandleMessage(ReceivedMessageEventArgs ea)
        {
            if (ea == null)
            {
                Trace.TraceWarning("Dispatcher HandleMessage: ReceivedMessageEventArgs is null");
                return;
            }
            var currentMsg = new MessageEntry(this, ea);
            if (currentMsg.IsRequest)
            {
                var methodName = ea.Properties?.ContentType;
                List<Action<MessageEntry>> handlers;
                if (!string.IsNullOrEmpty(methodName) && queryHandlers.TryGetValue(methodName,out handlers))
                {
                    foreach (var action in handlers)
                    {
                        action(currentMsg);
                    }
                }
                else
                {

                    if (currentMsg.IsViaValidForResponse())
                    {
                        currentMsg.ResponseError(new CoreException($"Handler [{methodName}] not declared for this service"))
                       .ContinueWith((res) =>
                       {
                           if (res.Exception != null)
                           {
                               Trace.TraceError(res.Exception.ToString());
                           }
                       });
                    }
                    else
                    {
                        Trace.TraceError($"Handler[{ methodName}] not declared for this service");
                    }
                   
                }
            }
            else
            {
                var last_via = currentMsg.via.GetLast();
                if (last_via != null)
                {
                    //callbacks
                    if (!string.IsNullOrEmpty(ea.Properties?.MessageId))
                    {
                        //todo: callbacks timeout
                        List<Action<string>> callbacks;
                        if (responceCallbacks.TryRemove(ea.Properties.MessageId, out callbacks) && callbacks!=null)
                        {
                            var data = Encoding.UTF8.GetString(ea.Content);
                            foreach (var cb in callbacks)
                            {
                                cb(data);
                            }                           
                        }
                    }
                    //resp handlers
                    if (!string.IsNullOrEmpty(last_via.responseHandlerName))
                    {
                        List<Action<MessageEntry, string>> handlers;
                        if (responceHandlers.TryGetValue(last_via.responseHandlerName, out handlers) && handlers!=null)
                        {                            
                            foreach (var action in handlers)
                            {
                                action(currentMsg, last_via.responseHandlerData);
                            }
                        }
                    }                  
                }
                ea.Ask();
            }            
        }

        //todo: null input
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

        //todo: timeout not implement
        public bool DeclareResponseCallback(string messageId, Action<string> callback, int? timeout)
        {
            if (callback == null)
            {
                throw new CoreException("Callback is null");
            }
            Trace.TraceInformation($"Declare response callback. MessageId={messageId}");
            List<Action<string>> handlers = null;
            if (responceCallbacks.TryGetValue(messageId, out handlers))
            {
                handlers.Add(callback);
                return true;
            }
            return responceCallbacks.TryAdd(messageId, new List<Action<string>> { callback });
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