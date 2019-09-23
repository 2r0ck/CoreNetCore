using CoreNetCore.Models;
using CoreNetCore.MQ;

namespace CoreNetCore.Helpers
{
    public abstract class MessageSenderBase
    {
        private readonly ICoreDispatcher _dispatcher;

        public MessageSenderBase(ICoreDispatcher dispatcher)
        {
            _dispatcher = dispatcher;
        }

        protected virtual MessageEntry CreateMessage(ReceivedMessageEventArgs receivedMessage = default(ReceivedMessageEventArgs))
        {
            return new MessageEntry(_dispatcher, receivedMessage);
        }
    }
}