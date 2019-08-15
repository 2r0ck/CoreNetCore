using System;

namespace CoreNetCore.Models
{
    /* Описывает тип, содержащий информацию о полученном сообщении для подписчика
    */

    public class MessageReceiveEventArgs
    {
        /// <summary>
        /// Содержание сообщения
        /// </summary>
        public byte[] Content { get; }

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
        public MessageReceiveEventArgs(byte[] content, Action ask, Action<bool> nask)
        {
            Content = content;
            Ask = ask;
            Nask = nask;
        }
    }
}