using CoreNetCore.Utils;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;

namespace CoreNetCore.Models
{
    /* Описывает тип, содержащий информацию о полученном сообщении для подписчика
    */

    public class ReceivedMessageEventArgs
    {
        public BasicDeliverEventArgs Info { get; }

        public IBasicProperties Properties => Info?.BasicProperties;

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
        public ReceivedMessageEventArgs(BasicDeliverEventArgs eventArgs, Action ask, Action<bool> nask)
        {
            Info = eventArgs;            
            Ask = ask;
            Nask = nask;
        }

        public string GetCorrelationId()
        {
            return Properties?.CorrelationId;
        }

        public string GetHeaderValue(string headerKey)
        {
            object result = null;
            if (Properties?.Headers?.TryGetValue(headerKey, out result) ?? false)
            {
                return result?.ToString();
            }
            return null;
        }

        public ViaContainer GetVia()
        {
            var viaHeaderStr = GetHeaderValue(MessageBasicPropertiesHeaders.VIA);
            return viaHeaderStr.FromJson<ViaContainer>();
        }
    }

    public sealed class MessageBasicPropertiesHeaders
    {
        public const string VIA = "Via";
        public const string VIA_TYPE = "Via_type";
        public const string DIRECTION = "Direction";
        public const string WORKKIND = "WorkKind";
        public const string WORKKIND_TYPE = "WorkKind_type";

        public const string DIRECTION_VALUE_RESPONSE = "RESPONSE";
        public const string DIRECTION_VALUE_REQUEST = "REQUEST";

        //TODO: Узнать что за константы.
        public const string VIA_TYPE_VALUE = "ru.spinosa.mq.dispatcher.message.via.Via";

        public const string WORKKIND_TYPE_VALUE = "ru.spinosa.enums.mq.MQWorkKind";
    }
}