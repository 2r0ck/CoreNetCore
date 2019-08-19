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
