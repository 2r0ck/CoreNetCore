using RabbitMQ.Client.Events;
using System;

namespace CoreNetCore.Models
{
    /* Описывает тип, содержащий информацию о полученном сообщении для подписчика
    */

    public class MessageReceiveEventArgs
    {

        public  BasicDeliverEventArgs Info { get; }

        /// <summary>
        /// Содержание сообщения
        /// </summary>
        public byte[] Content => Info?.Body;

        /// <summary>
        /// Сообщение об удачной обработке сообщения
        /// </summary>
        public Action Ask { get; }

        /// <summary>
        /// Сообщение об неудачной обработке сообщения. Принимает параметр, указывающий требуется ли повторно отправить сообщение
        /// </summary>
        public Action<bool> Nask { get; }
     

        /// <summary>
        /// тип, содержащий информацию о полученном сообщении для подписчика
        /// </summary>
        /// <param name="data">Содержание сообщения</param>
        /// <param name="ask">Сообщение об удачной обработке сообщения</param>
        /// <param name="nask">Сообщение об неудачной обработке сообщения. Принимает параметр, указывающий требуется ли повторно отправить сообщение</param>
        public MessageReceiveEventArgs(BasicDeliverEventArgs eventArgs, Action ask, Action<bool> nask)
        {
            Info = eventArgs;
            Ask = ask;
            Nask = nask;
        }
    }
}