using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace CoreNetCore.Models
{
    [Serializable]
    public class ChannelExchangeParam
    {
        public string Name { get; set; }

        public string Type { get; set; }

        public bool Durable { get; set; }

        public bool AutoDelete { get; set; }

        [JsonProperty("Arguments")]
        public IDictionary<string,object> Arguments { get; set; }
    }
}
