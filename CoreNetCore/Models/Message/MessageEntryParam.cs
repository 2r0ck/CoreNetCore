using System;
using System.Collections.Generic;
using System.Text;

namespace CoreNetCore.Models 
{
    [Serializable]
    public class MessageEntryParam
    {
        public string TransactionId { get; set; }

        public bool NeedRequestResolve { get; set; }

        public bool NeedResponseResolve { get; set; }

        public byte Priority { get; set; }

        public static MessageEntryParam GetDefault()
        {
            return new MessageEntryParam
            {
                NeedRequestResolve = true,
                Priority = 0
            };
        }
    }
}
