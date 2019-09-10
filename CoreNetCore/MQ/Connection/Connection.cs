using CoreNetCore.Configuration;
using CoreNetCore.Models;
using CoreNetCore.Utils;
using Microsoft.Extensions.Configuration;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Diagnostics;
using System.Text;
using System.Threading;

namespace CoreNetCore.MQ
{
    public class Connection : IDisposable, ICoreConnection
    {
        public bool IsConnected { get; private set; }
        public IAppId AppId { get; }

        public event Action<string> Disconnected;

        public event Action<string> Connected;

        public IModel Channel => channel;

        //#region config

        //private int maxRecoveryCount;
        //private int networkRecoveryInterval;
        //private ushort heartbeat;
        //private string host;
        //private bool default_durable;
        //private int port;
        //private string username;
        //private string password;
        //private ushort prefetch;

        //#endregion config

        #region disposed_objects

        private IConnection connection;
        private IModel channel;

        public IPrepareConfigService Configuration { get; }

        #endregion disposed_objects

        public Connection(IAppId appId, IPrepareConfigService configuration, IHealthcheck healthcheck)
        {
            Configuration = configuration;
            IsConnected = false;          
            AppId = appId;
            healthcheck.AddCheck(() => IsConnected);
        }

       
        /// <summary>
        /// Инициализирует подключение RabbitMQ
        /// </summary>
        public void Start()
        {
            Trace.TraceInformation("MQ Started..");
            Connect(1);
        }

        private void Connect(int attempt)
        {
            if (attempt > Configuration.MQ.maxRecoveryCount)
            {
                throw new CoreException("maxRecoveryCount reached.");
            }

            if (attempt > 1)
            {
                Thread.Sleep(Configuration.MQ.networkRecoveryInterval);
            }
            try
            {
                var factory = new ConnectionFactory()
                {
                    HostName = Configuration.MQ.host.host,
                    UserName = Configuration.MQ.host.mserv.username,
                    Password = Configuration.MQ.host.mserv.password
                };

                if (Configuration.MQ.host.port != 0)
                {
                    factory.Port = Configuration.MQ.host.port;
                }

                if (Configuration.MQ.heartbeat.HasValue)
                {
                    factory.RequestedHeartbeat = Configuration.MQ.heartbeat.Value;
                }

                Trace.TraceInformation($@"MQ Connecting [{attempt}] to [amqp://{factory.UserName}:{factory.Password}@{factory.HostName}:{factory.Port}/?heartbeat={factory.RequestedHeartbeat}");

                connection = factory.CreateConnection();
                connection.ConnectionShutdown += Connection_ConnectionShutdown;

                channel = connection.CreateModel();

                #region BasicQos help

                //qos:
                //This method requests a specific quality of service.
                //The QoS can be specified for the current channel or for all channels on the connection.
                //https://www.rabbitmq.com/amqp-0-9-1-reference.html

                //Parameters:

                //long prefetch-size
                //The client can request that messages be sent in advance so that when the client finishes processing a message, the following message is already held locally, rather than needing to be sent down the channel.Prefetching gives a performance improvement.

                //short prefetch-count
                //Specifies a prefetch window in terms of whole messages. This field may be used in combination with the prefetch-size field; a message will only be sent in advance if both prefetch windows(and those at the channel and connection level) allow it. The prefetch-count is ignored if the no - ack option is set.

                //bit global
                //RabbitMQ has reinterpreted this field.The original specification said: "By default the QoS settings apply to the current channel only. If this field is set, they are applied to the entire connection." Instead, RabbitMQ takes global = false to mean that the QoS settings should apply per - consumer(for new consumers on the channel; existing ones being unaffected) and global = true to mean that the QoS settings should apply per - channel.

                #endregion BasicQos help
                var prefetch = Configuration.MQ.prefetch.HasValue ? Configuration.MQ.prefetch.Value : (ushort)1;
                channel.BasicQos(0, prefetch, false);
                Trace.TraceInformation($"Prefetch: {prefetch}");
                this.IsConnected = true;
                Trace.TraceInformation($"MQ Connected.");
                Connected?.Invoke(AppId.CurrentUID);
            }
            catch (Exception ex)
            {
                Trace.TraceError(ex.ToString());
                Trace.TraceInformation("MQ Reconnect..");
                Connect(++attempt);
            }
        }

        /// <summary>
        /// Прослушивание очереди сообщений
        /// </summary>
        /// <param name="cparam">Парамеры создания обмена, очереди и подписчика</param>
        /// <param name="callback">Функция обратного вызова при получении сообщения</param>
        /// <returns></returns>
        public string Listen(ConsumerParam cparam, Action<ReceivedMessageEventArgs> callback)
        {            
            if (cparam?.QueueParam == null)
            {
                throw new CoreException("QueueParam not declared");
            }
            var default_durable = Configuration.MQ.durable.HasValue ? Configuration.MQ.durable.Value : false;
            channel.QueueDeclare(cparam.QueueParam.Name,
                cparam.QueueParam.Durable ?? default_durable,
                cparam.QueueParam.Exclusive,
                cparam.QueueParam.AutoDelete,
                cparam.QueueParam.Arguments);
            //Queue declare
            Trace.TraceInformation($"Declare queue [{cparam.QueueParam.Name}]. Options: {cparam.QueueParam.ToJson()}");

            if (cparam.ExchangeParam != null)
            {
                channel.ExchangeDeclare(cparam.ExchangeParam.Name,
                                       cparam.ExchangeParam.Type ?? ExchangeTypes.EXCHANGETYPE_DIRECT,
                                       cparam.ExchangeParam.Durable ?? default_durable,
                                       cparam.ExchangeParam.AutoDelete,
                                       cparam.ExchangeParam.Arguments);

                //Exchange declare
                Trace.TraceInformation($"Bind queue [{cparam.QueueParam.Name}] to exchange [{cparam.ExchangeParam.Name}({cparam.ExchangeParam.Type})]. AppId=[{AppId.CurrentUID}] ");

                //Bind queue and exchange
                channel.QueueBind(cparam.QueueParam.Name, cparam.ExchangeParam.Name, AppId.CurrentUID);
            }
            //create consumer and subscribe

            var consumer = new EventingBasicConsumer(channel);
            if (!string.IsNullOrEmpty(cparam.ConsumerTag))
            {
                consumer.ConsumerTag = consumer.ConsumerTag;
            }
            consumer.Received += (model, ea) =>
            {
                if (callback != null)
                {
                    //Ack Nack with try-catch? check if throw exception
                    var msg = new ReceivedMessageEventArgs(ea,
                    () =>
                    {
                        try
                        {
                            channel.BasicAck(ea.DeliveryTag, multiple: false);
                        }
                        catch (Exception ex)
                        {
                            Trace.TraceWarning($"Ask error:{ex}");
                        }
                    },
                    (autoRepit) =>
                    {
                        try
                        {
                            channel.BasicNack(ea.DeliveryTag, multiple: false, requeue: autoRepit);
                        }
                        catch (Exception ex)
                        {
                            Trace.TraceWarning($"Nask error:{ex}");
                        }
                    });

                    callback.Invoke(msg);
                }
            };

            return channel.BasicConsume(cparam.QueueParam.Name, cparam.AutoAck, consumer);           
        }

        /// <summary>
        /// Отмена подписчика по ConsumerTag
        /// </summary>
        /// <param name="consumerTag"></param>
        public void Cancel(string consumerTag)
        {
            try
            {
                channel.BasicCancel(consumerTag);
                Trace.TraceInformation($"Cancel consumer [{consumerTag}]");
            }
            catch (Exception ex)
            {
                throw new CoreException(ex);
            }
        }

        /// <summary>
        /// Рассылка сообщения
        /// </summary>
        /// <param name="pparam">Параметры сообщения</param>
        /// <param name="content">Сообщение</param>
        /// <param name="customProperties">Дополнительные параметры сообщения. Пример: Channel.CreateBasicProperties();</param>
        public void Publish(ProducerParam pparam, byte[] content, IBasicProperties customProperties)
        {
            try
            {
                var default_durable = Configuration.MQ.durable.HasValue ? Configuration.MQ.durable.Value : false;
                if (pparam?.ExchangeParam != null)
                {
                    //Exchange declare
                    channel.ExchangeDeclare(pparam.ExchangeParam.Name,
                        pparam.ExchangeParam.Type,
                        pparam.ExchangeParam.Durable ?? default_durable,
                        pparam.ExchangeParam.AutoDelete,
                        pparam.ExchangeParam.Arguments);
                }

                channel.BasicPublish(exchange: pparam?.ExchangeParam?.Name??"",
                routingKey: pparam?.RoutingKey ?? "",
                basicProperties: customProperties,
                body: content);

                //Trace.TraceInformation($"PUBLISH exchange:{ pparam?.ExchangeParam?.Name},routingKey:{pparam?.RoutingKey}, data:{Encoding.UTF8.GetString(content)}");
            }
            catch (Exception ex)
            {
                throw new CoreException(ex);
            }
        }

        private void Connection_ConnectionShutdown(object sender, ShutdownEventArgs e)
        {
            Trace.TraceWarning($"MQ Disconnected. ReplyCode = {e.ReplyCode}; ReplyText: {e.ReplyText}");
            Disconnected?.Invoke(AppId.CurrentUID);
            if (!_disposing)
            {
                Start();
            }
        }

        public IBasicProperties CreateChannelProperties()
        {
            return channel.CreateBasicProperties();
        }
        bool _disposing = false;
        public void Dispose()
        {
            _disposing = true;
            try
            {
             

                if (channel != null)
                {
                    Trace.TraceInformation($"MQ Channel closing. [{AppId.CurrentUID}]");
                    channel.Close();
                    channel.Dispose();
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError("Core connection dispose exception:");
                Trace.TraceError(ex.ToString());
            }

            try
            {
                if (connection != null)
                {
                    Trace.TraceInformation($"MQ Connection closing..[{AppId.CurrentUID}]");
                   
                    connection.Close();
                    connection.ConnectionShutdown -= Connection_ConnectionShutdown;
                    connection.Dispose();
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError("Core connection dispose exception:");
                Trace.TraceError(ex.ToString());
            }
        }
    }
}