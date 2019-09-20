using CoreNetCore.Core;
using CoreNetCore.MQ;
using System;

namespace CoreNetCore.Helpers
{
    public abstract class ResponseHandlerBase : MessageSenderBase, IMessageHandler, IRegisterHandler
    {
        protected readonly ICoreDispatcher _dispatcher;

        public abstract string HandlerName { get; }

        public abstract Action<MessageEntry, string> Handler { get; }

        public ResponseHandlerBase(ICoreDispatcher dispatcher) : base(dispatcher)
        {
            this._dispatcher = dispatcher;
        }

        public virtual void Register()
        {
            _dispatcher.DeclareResponseHandler(HandlerName, Handler);
        }
    }
}