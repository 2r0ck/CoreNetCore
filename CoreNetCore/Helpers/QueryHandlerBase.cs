using CoreNetCore.Core;
using CoreNetCore.MQ;
using System;

namespace CoreNetCore.Helpers
{
    public abstract class QueryHandlerBase : MessageSenderBase, IMessageHandler, IRegisterHandler
    {
        protected readonly ICoreDispatcher _dispatcher;

        public abstract string HandlerName { get; }

        public abstract Action<MessageEntry> Handler { get; }

        public QueryHandlerBase(ICoreDispatcher dispatcher) : base(dispatcher)
        {
            this._dispatcher = dispatcher;
        }

        public virtual void Register()
        {
            _dispatcher.DeclareQueryHandler(HandlerName, Handler);
        }
    }
}