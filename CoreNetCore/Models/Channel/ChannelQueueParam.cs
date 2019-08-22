using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace CoreNetCore.Models 
{
    [Serializable]
    public class ChannelQueueParam
    {
        /// <summary>
        /// Имя очереди
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Использовать только одно подключение в очередь
        /// </summary>
        public bool Exclusive { get; set; }

        /// <summary>
        /// Автоматически постанавливается после рестарта
        /// </summary>
        public bool Durable { get; set; }

        /// <summary>
        /// Автоматически удаляется после удаления последнего подписчика 
        /// </summary>
        public bool AutoDelete { get; set; }

        [JsonProperty("Arguments")]
        public IDictionary<string, object> Arguments { get; set; }

        public ChannelQueueParam()
        {
            //TODO: expires param ?
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
