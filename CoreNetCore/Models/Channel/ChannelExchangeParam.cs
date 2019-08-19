using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace CoreNetCore.Models
{
    [Serializable]
    /*Настройки для создания обмена.
    */
    public class ChannelExchangeParam
    {
        /// <summary>
        /// Имя обмена
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Тип обмена (значения ExchangeTypes.EXCHANGETYPE_*)
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// Автоматически восстанавливает обмен после рестарта
        /// </summary>
        public bool Durable { get; set; }

        /// <summary>
        /// Автоматически удаляется если закрыта последняя очередь
        /// </summary>
        public bool AutoDelete { get; set; }

        [JsonProperty("Arguments")]
        public IDictionary<string,object> Arguments { get; set; }
    }
}
