using CoreNetCore.Models;
using CoreNetCore.Utils;
using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Text;
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

                IsRequest = receivedMessage.GetHeaderValue(MessageBasicPropertiesHeaders.DIRECTION) != MessageBasicPropertiesHeaders.DIRECTION_VALUE_RESPONSE;
                if (via == null)
                {
                    via = new ViaContainer()
                    {
                        queue = new Stack<ViaElement>()
                    };

                    if (receivedMessage.Properties != null)
                    {
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

        public Task<DataArgs<ViaElement>> RequestAsync<T>(string serviceName, string exchangeType, string queryName, DataArgs<T> queryData, Action<string> callback, MessageEntryParam parameters)
        {
            return RequestAsync(serviceName, exchangeType, queryName, queryData, null, null, callback, parameters);
        }

        //public Task<CallbackMessageEventArgs<ViaElement>> RequestAsync(string serviceName, string exchangeType, string queryName, object queryData, string responceHandlerName, object responseHandlerData, MessageEntryParam parameters)
        //{
        //    var responseHandlerDataStr = responseHandlerData?.ToJson(true);
        //    return RequestAsync(serviceName, exchangeType, queryName, queryData, responceHandlerName, responseHandlerDataStr, null, parameters);
        //}

        public Task<DataArgs<ViaElement>> RequestAsync<T>(string serviceName, string exchangeType, string queryName, DataArgs<T> queryData, string responceHandlerName, string responseHandlerData, MessageEntryParam parameters)
        {
            return RequestAsync(serviceName, exchangeType, queryName, queryData, responceHandlerName, responseHandlerData, null, parameters);
        }


        private Task<DataArgs<ViaElement>> RequestAsync<T>(string serviceName, string exchangeType, string queryName, DataArgs<T> queryData, string responceHandlerName, string responseHandlerData, Action<string> callback, MessageEntryParam parameters)
        {
            var responseResolve = parameters?.NeedResponseResolve ?? false;

            //TODO: подумать ExchangeTypes.EXCHANGETYPE_DIRECT для responceHandlers?
            var responseKind = callback == null ? ExchangeTypes.EXCHANGETYPE_FANOUT : ExchangeTypes.EXCHANGETYPE_DIRECT;

            var replayTo = responseResolve ? Dispatcher.SelfServiceName : Dispatcher.GetConnectionByExchangeType(responseKind);
            var queryDataJson = queryData.ToJson();

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
            properties.CorrelationId = ReceivedMessage?.Properties?.CorrelationId ?? parameters?.TransactionId ?? Guid.NewGuid().ToString();
            properties.ContentType = currentViaElement.queryHandlerName;
            properties.MessageId = currentViaElement.messageId;
            properties.ReplyTo = currentViaElement.replyTo;
            properties.Priority = currentViaElement.priority == 0 ? (byte)1 : currentViaElement.priority;
            properties.Headers = new Dictionary<string, object>();
            properties.Headers.TryAdd(MessageBasicPropertiesHeaders.VIA, this.via.ToJson());
            properties.Headers.TryAdd(MessageBasicPropertiesHeaders.DIRECTION, MessageBasicPropertiesHeaders.DIRECTION_VALUE_REQUEST);
            properties.Headers.TryAdd(MessageBasicPropertiesHeaders.WORKKIND, responseKind);
            properties.Headers.TryAdd(MessageBasicPropertiesHeaders.WORKKIND_TYPE, MessageBasicPropertiesHeaders.WORKKIND_TYPE_VALUE);
            properties.Headers.TryAdd(MessageBasicPropertiesHeaders.VIA_TYPE, MessageBasicPropertiesHeaders.VIA_TYPE_VALUE);

            exchangeType = ExchangeTypes.Get(exchangeType);

            var link = LinkTypes.GetLinkByExchangeType(exchangeType);

            var routingKey = string.Empty;

            if (exchangeType == ExchangeTypes.EXCHANGETYPE_FANOUT)
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

            return Push(currentViaElement, parameters?.NeedRequestResolve ?? true, link, serviceName, exchangeName, routingKey, exchangeType, queryDataJson, properties)
                .ContinueWith((result) =>
                {
                    if (callback != null)
                    {
                        if (result.Exception != null)
                        {
                            callback(new DataArgs<object>(result.Exception).ToJson());
                        }
                        else
                        {
                            Dispatcher.DeclareResponseCallback(currentViaElement.messageId, callback, parameters?.Timeout);
                        }
                    }
                    return result.Result;
                });
        }


        //public Task<DataArgs<ViaElement>> ResponseOk<T>(T data)
        //{
        //    var dataArgs = new DataArgs<T>(data);
        //    return ResponseOk<T>(dataArgs);
        //}

        public Task<DataArgs<ViaElement>> ResponseOk<T>(DataArgs<T> data) 
        {
            var data_str = data.ToJson(true);
            return Response(data_str);
        }

        public Task<DataArgs<ViaElement>> ResponseError(Exception ex)
        {
            var dataArgs = new DataArgs<object>(ex);       
            return ResponseError(dataArgs);
        }

        public Task<DataArgs<ViaElement>> ResponseError<T>(DataArgs<T> data)
        {
            data.result = false;
            var data_str = data.ToJson(true);
            return Response(data_str);
        }

        private Task<DataArgs<ViaElement>> Response(string data)
        {
            var lastVia = via?.GetLast();
            if (lastVia == null)
            {
                throw new CoreException("Responce message error:  via element is null");
            }

            var properties = Dispatcher.Connection.CreateChannelProperties();
            properties.CorrelationId = ReceivedMessage?.Properties?.CorrelationId;
            properties.ContentType = lastVia.queryHandlerName;
            properties.MessageId = lastVia.messageId;
            properties.Priority = lastVia.priority == 0 ? (byte)1 : lastVia.priority;
            properties.Type = lastVia.mqWorkKind;
            properties.Headers = new Dictionary<string, object>();
            properties.Headers.TryAdd(MessageBasicPropertiesHeaders.VIA, this.via.ToJson());
            properties.Headers.TryAdd(MessageBasicPropertiesHeaders.DIRECTION, MessageBasicPropertiesHeaders.DIRECTION_VALUE_RESPONSE);
            properties.Headers.TryAdd(MessageBasicPropertiesHeaders.WORKKIND, null);
            properties.Headers.TryAdd(MessageBasicPropertiesHeaders.VIA_TYPE, MessageBasicPropertiesHeaders.VIA_TYPE_VALUE);

            var link = LinkTypes.GetLinkByExchangeType(lastVia.mqWorkKind);

            var routeKey = string.Empty;
            switch (lastVia.mqWorkKind)
            {
                case ExchangeType.Fanout: routeKey = lastVia.replyTo; break;
                case ExchangeType.Direct: routeKey = lastVia.appId; break;
                case ExchangeType.Topic: routeKey = lastVia.routeKey; break;
            }

            var exchange = lastVia.mqWorkKind == ExchangeType.Fanout ? string.Empty : lastVia.replyTo;
            var doResolve = lastVia.doResolve;

            return Push(lastVia, doResolve, link, lastVia.replyTo, exchange, routeKey, lastVia.mqWorkKind, data, properties)
                .ContinueWith((res) =>
                {
                    if (res.Exception != null)
                    {
                        ReceivedMessage?.Nask(false);
                        throw res.Exception;
                    }
                    ReceivedMessage?.Ask();
                    return res.Result;
                });
        }

        public bool IsViaValidForResponse()
        {
            var lastVia = via?.GetLast();
            if (lastVia == null) return false;
            if (lastVia.doResolve && string.IsNullOrEmpty(lastVia.replyTo))
            {
                return false;
            }

            if (!lastVia.doResolve && string.IsNullOrEmpty(lastVia.replyTo) && string.IsNullOrEmpty(lastVia.appId))
            {
                return false;
            }
            return true;
        }

        private Task<DataArgs<ViaElement>> Push(ViaElement via, bool requestResolve, string link, string service, string exchangeName, string routingKey, string exchangeType, string queryData, IBasicProperties properties)
        {
            DataArgs<ViaElement> messageEventArgs = new DataArgs<ViaElement>();

            if (requestResolve)
            {
                //Для начала надо узнать в какие очереди посылать запросы
                return this.Dispatcher.Resolve(service, link)
                      .ContinueWith(resolver_link =>
                      {
                          //try
                          //{
                          if (resolver_link.Exception != null)
                          {
                              messageEventArgs.SetException(resolver_link.Exception);
                          }
                          var link_str = resolver_link.Result;
                          var exchange = ExchangeTypes.GetExchangeName(link_str, null, exchangeType);
                          bool isFanout = ExchangeType.Fanout.Equals(exchangeType, StringComparison.CurrentCultureIgnoreCase);

                          var rk = isFanout ? exchange : routingKey;

                          var options = new ProducerParam()
                          {
                              RoutingKey = rk                               
                          };

                          if (!isFanout)
                          {
                              options.ExchangeParam = new ChannelExchangeParam()
                              {
                                  Name = exchange,
                                  Type = exchangeType ?? ExchangeTypes.EXCHANGETYPE_FANOUT
                              };
                          }

                          var data = Encoding.UTF8.GetBytes(queryData);
                          //посылаем сообщение
                          Dispatcher.Connection.Publish(options, data, properties);
                          messageEventArgs.result = true;
                          messageEventArgs.data = via;
                          //}
                          //catch (Exception ex)
                          //{
                          //    messageEventArgs.SetException(ex);
                          //}
                          return messageEventArgs;
                      });
            }
            else
            {
                return Task.Run(() =>
                {
                    //try
                    //{
                    //для запросов, где очереди известны
                    var options = new ProducerParam()
                    {
                        RoutingKey = routingKey
                    };
                    if (!string.IsNullOrEmpty(exchangeName))
                    {
                        options.ExchangeParam = new ChannelExchangeParam()
                        {
                            Name = exchangeName,
                            Type = exchangeType ?? ExchangeTypes.EXCHANGETYPE_FANOUT
                        };
                    }

                    var data = Encoding.UTF8.GetBytes(queryData);
                    //посылаем сообщение
                    Dispatcher.Connection.Publish(options, data, properties);
                    //}
                    //catch (Exception ex)
                    //{
                    //    messageEventArgs.result = false;
                    //    messageEventArgs.SetException(ex);
                    //}
                    return messageEventArgs;
                });
            }
        }
    }
}