using System;
using System.Collections.Generic;
using System.Text;

namespace CoreNetCore.Models
{
    [Serializable]
    /*  Класс, описывающий настройки для 
     *  создания подписчика на очередь RabbitMQ
     *  ИНФО: https://www.rabbitmq.com/tutorials/amqp-concepts.html
    */
    public class ConsumerParam
    {
        /// <summary>
        /// Тип прямого обмена
        /// </summary>
        public const string EXCHANGETYPE_DIRECT = "Direct";

        /// <summary>
        /// Тип обмена Fanout направляет сообщения во все связанные очереди без разбора.
        /// </summary>
        public const string EXCHANGETYPE_FANOUT = "Fanout";

        /// <summary>
        /// Тип обмена Тема направляет сообщения в очереди, ключ маршрутизации которых совпадает со всеми или частью ключа маршрутизации. 
        /// </summary>
        public const string EXCHANGETYPE_TOPIC = "Topic";

        /// <summary>
        /// Тип обмена заголовками направляет сообщения на основе сопоставления заголовков сообщений с ожидаемыми заголовками, указанными в очереди привязки.
        /// </summary>
        public const string EXCHANGETYPE_HEADERS = "Headers";

        /// <summary>
        /// Настройки для создания обмена. 
        /// </summary>
        public ChannelExchangeParam ExchangeParam { get; set; }
        
        /// <summary>
        /// Настройки для создания очереди
        /// </summary>
        public ChannelQueueParam QueueParam { get; set; }

        /// <summary>
        /// Идентификатор подписчика 
        /// </summary>
        public string ConsumerTag { get; set; }

        /// <summary>
        /// Автоматически отсылать отчеты о получении при получении сообщения 
        /// </summary>
        public bool AutoAck { get; set; }
        
    }
}
