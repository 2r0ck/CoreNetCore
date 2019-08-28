using CoreNetCore.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace CoreNetCore.MQ 
{
    [Serializable]
    public class MessageEntry  : IMessageEntry
    {
        public ICoreDispatcher Dispatcher { get; }

        public MessageEntry(ICoreDispatcher dispatcher)
        {
            Dispatcher = dispatcher;
        }
 


        public Task RequestAsync(string serviceName, string exchangeType, string queryName, object queryData, MessageEntryParam parameters)
        {
            return null;
        }
    }
}
