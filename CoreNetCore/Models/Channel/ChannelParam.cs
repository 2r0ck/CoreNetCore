using System;
using System.Collections.Generic;
using System.Text;

namespace CoreNetCore.Models
{
    [Serializable]
    public class ChannelParam
    {
        public const string EXCHANGETYPE_DIRECT = "Direct";
        public const string EXCHANGETYPE_FANOUT = "Fanout";
        public const string EXCHANGETYPE_TOPIC = "Topic";
        public const string EXCHANGETYPE_HEADERS = "Headers";

        public ChannelExchangeParam ExchangeParam { get; set; }

        public ChannelQueueParam QueueParam { get; set; }

        public string ConsumerTag { get; set; }
    }
}
