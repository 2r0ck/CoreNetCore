using CoreNetCore.Models;
using CoreNetCore.Utils;
using Microsoft.Extensions.Configuration;
using RabbitMQ.Client;
using System;
using System.Diagnostics;
using System.Threading;

namespace CoreNetCore.MQ
{
    public class Connection : IDisposable
    {
        public bool IsConnected { get; private set; }
        public IAppId AppId { get; }

        public event Action<string> Disconnected;

        public event Action<string> Connected;

        #region config

        private int maxRecoveryCount;
        private int networkRecoveryInterval;
        private ushort heartbeat;
        private string host;
        private int port;
        private string username;
        private string password;
        private ushort prefetch;

        #endregion config

        #region disposed_objects 
        private IConnection connection;
        private IModel channel;

        #endregion

        public Connection(IAppId appId,IConfiguration configuration)
        {
            IsConnected = false;
            ReadConfig(configuration);
            AppId = appId;
        }

        private void ReadConfig(IConfiguration configuration)
        {
            maxRecoveryCount = 1;
            var warn = "Config key [{0}] not declared.";
            if (!int.TryParse(configuration.GetStrValue("mq:maxRecoveryCount"), out maxRecoveryCount))
            {
                Trace.TraceWarning(string.Format(warn, "mq:maxRecoveryCount"));
            }
            networkRecoveryInterval = 0;
            if (!int.TryParse(configuration.GetStrValue("mq:networkRecoveryInterval"), out networkRecoveryInterval))
            {
                Trace.TraceWarning(string.Format(warn, "mq:networkRecoveryInterval"));
            }

            heartbeat = 0;
            if (!ushort.TryParse(configuration.GetStrValue("mq.heartbeat"), out heartbeat))
            {
                Trace.TraceWarning(string.Format(warn, "mq.heartbeat"));
            }           

            port = 0;
            if (!int.TryParse(configuration.GetStrValue("mq:host:port"), out port))
            {
                Trace.TraceWarning(string.Format(warn, "mq:host:port"));
            }

            prefetch = 1;
            if (!ushort.TryParse(configuration.GetStrValue("mq:prefetch"), out prefetch))
            {
                Trace.TraceWarning(string.Format(warn, "mq:prefetch"));
            }

            host = configuration.GetStrValue("mq:host:host", true);
            username = configuration.GetStrValue("mq:host:mserv:username", true);
            password = configuration.GetStrValue("mq:host:mserv:password", true);
        }

        public void Start()
        {
            Connect(1);
        }

        private void Connect(int attempt)
        {
            if (attempt > maxRecoveryCount)
            {
                throw new CoreException("maxRecoveryCount reached.");
            }

            if (attempt > 1)
            {
                Thread.Sleep(networkRecoveryInterval);
            }
            try
            {
                var factory = new ConnectionFactory()
                {
                    HostName = host,
                    UserName = username,
                    Password = password
                };

                if (port != 0)
                {
                    factory.Port = port;
                }

                if (heartbeat != 0)
                {
                    factory.RequestedHeartbeat = heartbeat;
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
                #endregion

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

        public bool Listen(ChannelParam channelParam,Action<byte[]> callback)
        {
            try
            {
                if(channelParam?.QueueParam == null)
                {
                    throw new CoreException("QueueParam not declared");
                }

                Trace.TraceInformation($"Creating queue [{channelParam.QueueParam.Name}]. Options: {channelParam.QueueParam.ToJson()}");
                channel.QueueDeclare(channelParam.QueueParam.Name, channelParam.QueueParam.Durable, channelParam.QueueParam.Exclusive, channelParam.QueueParam.AutoDelete, channelParam.QueueParam.Arguments);

                return true;
            }
            catch (Exception ex)
            {
                throw new CoreException(ex);
            }
        }




        private void Connection_ConnectionShutdown(object sender, ShutdownEventArgs e)
        {
            Trace.TraceWarning("MQ Disconnected. MQ Starting..");
            Disconnected?.Invoke(AppId.CurrentUID);            
            Start();
        }

        public void Dispose()
        {
            try
            {
                if (channel != null)
                {
                    Trace.TraceInformation("MQ Channel closing..");
                    channel.Close();
                    channel.Dispose();
                }

                if (connection != null)
                {
                    Trace.TraceInformation("MQ Connection closing..");
                    connection.ConnectionShutdown -= Connection_ConnectionShutdown;
                    connection.Close();
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