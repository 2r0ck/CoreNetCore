using CoreNetCore.Models;
using CoreNetCore.Utils;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CoreNetCore.MQ
{
    public class MessageEntry // : IMessageEntry
    {
        public ICoreDispatcher Dispatcher { get; }

        /// <summary>
        /// Состояние тцепочек запросов (хлебные крошки)
        /// </summary>
        public readonly ViaContainer via;

        public ReceivedMessageEventArgs ReceivedMessage { get; }

        public bool IsRequest { get; set; }

        public MessageEntry(ICoreDispatcher dispatcher, ReceivedMessageEventArgs receivedMessage)
        {
            ReceivedMessage = receivedMessage;

            Dispatcher = dispatcher;

            if (receivedMessage != null)
            {
                via = receivedMessage.GetVia();
                if (via == null)
                {
                    via = new ViaContainer()
                    {
                        queue = new Stack<ViaElement>()
                    };

                    if (receivedMessage.Properties != null)
                    {
                        IsRequest = ExchangeTypes.Get(receivedMessage.GetHeaderValue(MessageBasicPropertiesHeaders.DIRECTION)) != MessageBasicPropertiesHeaders.DIRECTION_VALUE_RESPONSE;

                        via.queue.Push(new ViaElement()
                        {
                            appId = receivedMessage.Properties.AppId,
                            messageId = receivedMessage.Properties.MessageId,
                            mqWorkKind = ExchangeTypes.Get(receivedMessage.GetHeaderValue(MessageBasicPropertiesHeaders.WORKKIND)) ?? ExchangeTypes.EXCHANGETYPE_FANOUT,
                            priority = receivedMessage.Properties.Priority,
                            queryHandlerName = receivedMessage.Properties.ContentType,
                            replyTo = receivedMessage.Properties.ReplyTo
                        });
                    }
                }
            }
            else
            {
                via = new ViaContainer()
                {
                    queue = new Stack<ViaElement>()
                };
            }
        }

        public Task RequestAsync(string serviceName, string exchangeType, string queryName, object queryData, Action<CallbackMessageEventArgs> callback, MessageEntryParam parameters)
        {
            return RequestAsync(serviceName, exchangeType, queryName, queryData, null, null, callback, parameters);
        }

        public Task RequestAsync (string serviceName, string exchangeType, string queryName, object queryData, string responceHandlerName, object responseHandlerData, MessageEntryParam parameters) 
        {
            var responseHandlerDataStr = responseHandlerData?.ToJson(true);
            return RequestAsync(serviceName, exchangeType, queryName, queryData, responceHandlerName, responseHandlerDataStr, null, parameters);
        }

        public Task RequestAsync(string serviceName, string exchangeType, string queryName, object queryData, string responceHandlerName, string responseHandlerData, MessageEntryParam parameters)
        {
            return RequestAsync(serviceName, exchangeType, queryName, queryData, responceHandlerName, responseHandlerData, null, parameters);
        }

        private Task RequestAsync(string serviceName, string exchangeType, string queryName, object queryData, string responceHandlerName, string responseHandlerData, Action<CallbackMessageEventArgs> callback, MessageEntryParam parameters)
        {
            var responseResolve = parameters?.NeedResponseResolve ?? false;

            //TODO: подумать ExchangeTypes.EXCHANGETYPE_DIRECT для responceHandlers?
            var responseKind = callback == null ?  ExchangeTypes.EXCHANGETYPE_FANOUT:ExchangeTypes.EXCHANGETYPE_DIRECT;

            var replayTo = responseResolve ? Dispatcher.SelfServiceName : Dispatcher.GetConnectionByExchangeType(responseKind);

            //записываем  текущее состояние запроса 
            var currentViaElement = new ViaElement()
            {
                replyTo = replayTo,
                mqWorkKind = responseKind,
                messageId = Guid.NewGuid().ToString(),
                queryHandlerName = queryName,
                appId = Dispatcher.AppId,
                priority = parameters?.Priority ?? 0,
                doResolve = responseResolve,
                responseHandlerName = responceHandlerName,
                responseHandlerData = responseHandlerData
            };
            this.via.queue.Push(currentViaElement);

            var properties = Dispatcher.Connection.CreateChannelProperties();
            properties.CorrelationId = ReceivedMessage?.Properties?.CorrelationId ?? parameters.TransactionId ?? Guid.NewGuid().ToString();
            properties.ContentType = currentViaElement.queryHandlerName;
            properties.MessageId = currentViaElement.messageId;
            properties.ReplyTo = currentViaElement.replyTo;
            properties.Priority = currentViaElement.priority == 0? (byte)1 : currentViaElement.priority;
            properties.Headers = new Dictionary<string, object>();
            properties.Headers.TryAdd(MessageBasicPropertiesHeaders.VIA, this.via.ToJson());
            properties.Headers.TryAdd(MessageBasicPropertiesHeaders.DIRECTION, MessageBasicPropertiesHeaders.DIRECTION_VALUE_REQUEST);
            properties.Headers.TryAdd(MessageBasicPropertiesHeaders.WORKKIND, responseKind);
            properties.Headers.TryAdd(MessageBasicPropertiesHeaders.WORKKIND_TYPE, MessageBasicPropertiesHeaders.WORKKIND_TYPE_VALUE);
            properties.Headers.TryAdd(MessageBasicPropertiesHeaders.VIA_TYPE, MessageBasicPropertiesHeaders.VIA_TYPE_VALUE);


            var link = LinkTypes.GetLinkByExchangeType(exchangeType);

            var routingKey = string.Empty;

            if (ExchangeTypes.Get(exchangeType) == ExchangeTypes.EXCHANGETYPE_FANOUT)
            {
                routingKey = serviceName;
            }
            else if (responseKind == ExchangeTypes.EXCHANGETYPE_DIRECT)
            {
                routingKey = parameters?.AppId;
                if (string.IsNullOrEmpty(routingKey))
                {
                    throw new CoreException("Relation [Direct to Direct] - AppId must be initialized");
                }

            }

            var exchangeName = exchangeType == ExchangeTypes.EXCHANGETYPE_FANOUT ? "" : serviceName;

            return Push().ContinueWith((result)=> {
                //тема с таймаутом
                //если есть ошибки и есть колбек запускаем его с ошибками
                //пробрасываем таск дальше
                //....
            
            });
        }


        public Task Push()
        {
            return null;
        }
    }
}