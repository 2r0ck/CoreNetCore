using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace CoreNetCore.Models 
{
    [Serializable]
    public class ChannelQueueParam
    {
        public string Name { get; set; }

        public bool Exclusive { get; set; }

        public bool Durable { get; set; }

        public bool AutoDelete { get; set; }
        [JsonProperty("Arguments")]
        public IDictionary<string, object> Arguments { get; set; }

        public ChannelQueueParam()
        {

        }

        public ChannelQueueParam(string name = "", bool durable = false,bool exclusive = true,bool autoDelete=  true, IDictionary<string,object> arguments=null)
        {
            Name = name;
            Durable = durable;
            Exclusive = exclusive;
            AutoDelete = autoDelete;
            Arguments = arguments;
        }
    }
}
