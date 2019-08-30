using System;
using System.Collections.Generic;
using System.Text;

namespace CoreNetCore.Models 
{
    [Serializable]
    public class MessageEntryParam
    {
        public string TransactionId { get; set; }

        public bool? NeedRequestResolve { get; set; }

        public bool NeedResponseResolve { get; set; }

        public byte Priority { get; set; }

        public string AppId { get; set; }

        public int? Timeout { get; set; }
    }
}
