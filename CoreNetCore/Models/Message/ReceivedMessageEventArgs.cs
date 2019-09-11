using CoreNetCore.Utils;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Text;

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
        public Action Ack { get; }

        /// <summary>
        /// Сообщение об неудачной обработке сообщения. Принимает параметр, указывающий требуется ли повторно отправить сообщение
        /// </summary>
        public Action<bool> Nack { get; }

        /// <summary>
        /// тип, содержащий информацию о полученном сообщении для подписчика
        /// </summary>
        /// <param name="eventArgs">Информация о сообщении</param>
        /// <param name="ack">Сообщение об удачной обработке сообщения</param>
        /// <param name="nack">Сообщение об неудачной обработке сообщения. Принимает параметр, указывающий требуется ли повторно отправить сообщение</param>
        public ReceivedMessageEventArgs(BasicDeliverEventArgs eventArgs, Action ack, Action<bool> nack)
        {
            Info = eventArgs;            
            Ack = ack;
            Nack = nack;
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
                if(result is byte[])
                {
                    return Encoding.UTF8.GetString((byte[])result);
                }
                return result?.ToString();
            }
            return null;
        }

        public ViaContainer GetVia()
        {
            var viaHeaderStr = GetHeaderValue(MessageBasicPropertiesHeaders.VIA);
            return viaHeaderStr.FromJson<ViaContainer>();
        }

        public T GetMessageData<T>() where T:class
        {
            if (Content != null)
            {
                var data_str = Encoding.UTF8.GetString(Content);
                return data_str.FromJson<T>();
            }

            return null;
        }

        public string GetMessageContentString()
        {
            if (Content != null)
            {
                var data_str = Encoding.UTF8.GetString(Content);
                return data_str;
            }

            return null;
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