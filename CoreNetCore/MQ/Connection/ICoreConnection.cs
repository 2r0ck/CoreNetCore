using System;
using System.Threading.Tasks;
using CoreNetCore.Models;
using RabbitMQ.Client;

namespace CoreNetCore.MQ
{
    public interface ICoreConnection
    {
        IAppId AppId { get; }
        IModel Channel { get; }
        bool IsConnected { get; }

        event Action<string> Connected;
        event Action<string> Disconnected;

        void Cancel(string consumerTag);
        void Dispose();
        Task<string> Listen(ConsumerParam cparam, Action<ReceivedMessageEventArgs> callback);
        void Publish(ProducerParam pparam, byte[] content, IBasicProperties customProperties);
        IBasicProperties CreateChannelProperties();
        void Start();
    }
}