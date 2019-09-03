using CoreNetCore.Configuration;
using CoreNetCore.Models;
using CoreNetCore.Utils;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace CoreNetCore.MQ 
{
    public class CoreDispatcher : ICoreDispatcher
    {
        ConcurrentDictionary<string, List<Action<MessageEntry>>> queryHandlers = new ConcurrentDictionary<string, List<Action<MessageEntry>>>();
        ConcurrentDictionary<string, List<Action<MessageEntry, string>>> responceHandlers = new ConcurrentDictionary<string, List<Action<MessageEntry, string>>>();

        //CallbackMessageEventArgs
        ConcurrentDictionary<string, List<Action<CallbackMessageEventArgs<object>>>> responceCallbacks = new ConcurrentDictionary<string, List<Action<CallbackMessageEventArgs<object>>>>();

        public string SelfServiceName => $"{Config.Starter._this._namespace}:{Config.Starter._this.servicename}:{Config.Starter._this.majorversion}";
        public string ExchangeConnectionString { get; }

        public string QueueConnectionString { get; }

        public IPrepareConfigService Config { get; }

        public string AppId { get; }
        public ICoreConnection Connection { get;}
        public IResolver Resolver { get; }

        bool running = false;

        public CoreDispatcher(IPrepareConfigService config, IAppId appId, ICoreConnection coreConnection, IResolver resolver, IHealthcheck healthcheck)
        {
            Config = config;
            AppId = appId.CurrentUID;
            Connection = coreConnection;
            Resolver = resolver;
            healthcheck.AddCheck(() => running);
            Resolver.Stopped+=(appid) => { running = false; };
            Resolver.Started += Resolver_Started;
        }

        private void Resolver_Started(string obj)
        {
            try
            {
                //TODO: Self resolving timeout, exiting ...
                Trace.TraceInformation("Resolver started");

                var links = Resolver.RegisterSelf().Result;
                Trace.TraceInformation("Self links resolved");

                if (links != null)
                {
                    foreach (var link in links)
                    {

                    }
                }



            }
            catch (Exception ex)
            {

                throw;
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
        public bool DeclareResponseCallback(string messageId, Action<CallbackMessageEventArgs<object>> callback,int? timeout)
        {
            if (callback == null)
            {
                throw new CoreException("Callback is null");
            }
            Trace.TraceInformation($"Declare response callback. MessageId={messageId}");
            List<Action<CallbackMessageEventArgs<object>>> handlers = null;
            if (responceCallbacks.TryGetValue(messageId, out handlers))
            {
                handlers.Add(callback);
                return true;
            }
            return responceCallbacks.TryAdd(messageId, new List<Action<CallbackMessageEventArgs<object>>> { callback });
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
