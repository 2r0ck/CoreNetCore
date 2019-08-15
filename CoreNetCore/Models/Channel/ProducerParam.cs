using System;
using System.Collections.Generic;
using System.Text;

namespace CoreNetCore.Models
{
    /*  Класс, описывающий настройки для 
   *  создания публикатора в очередь RabbitMQ
   *  ИНФО: https://www.rabbitmq.com/tutorials/amqp-concepts.html
  */
    public class ProducerParam
    {
        /// <summary>
        /// Настройки для создания обмена. 
        /// </summary>
        public ChannelExchangeParam ExchangeParam { get; set; }


        /// <summary>
        /// Маркер публикации
        /// </summary>
        public string RoutingKey { get; set; }
    }
}
