using CoreNetCore.Models;
using System;
using System.Collections.Generic;

namespace CoreNetCore.MQ
{
    public interface ICoreDispatcher
    {

        string AppId { get; }
        ICoreConnection Connection { get; }

        bool DeclareQueryHandler(string actionName, Action<MessageEntry> handler);

        bool DeclareResponseHandler(string actionName, Action<MessageEntry, string> handler);

        bool DeclareResponseCallback(string messageId, Action<CallbackMessageEventArgs<object>> callback,int? timeout);

        string SelfServiceName { get; }
        /// <summary>
        /// connection for query, kind = direct
        /// </summary>
        string ExchangeConnectionString { get; }
        
        /// <summary>
        /// connection for query, kind=fanout
        /// </summary>
        string QueueConnectionString { get; }

        string GetConnectionByExchangeType(string exchangeKind);

        string Resolve(string service, string type);
    }
}