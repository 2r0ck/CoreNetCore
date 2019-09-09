using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace CoreNetCore.Models
{
    [Serializable]
    public class ViaContainer
    {
       public  Stack<ViaElement> queue { get; set; }

        public ViaElement GetLast()
        {
            return queue?.Peek();
        }
    }



    [Serializable]
    public class ViaElement
    {
        public string replyTo { get; set; }
        public string appId { get; set; }
        public string routeKey { get; set; }
        public string mqWorkKind { get; set; }
        public string messageId { get; set; }

        [JsonProperty("type")]
        public string queryHandlerName { get; set; }  

        public byte priority { get; set; }

        public bool doResolve { get; set; }

        [JsonProperty("handlerMethod")]
        public string responseHandlerName { get; set; }

        [JsonProperty("context")]
        public object responseHandlerData { get; set; }

    }
}
