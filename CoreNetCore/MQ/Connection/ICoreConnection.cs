using System;
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
        string Listen(ConsumerParam cparam, Action<MessageReceiveEventArgs> callback);
        void Publish(ProducerParam pparam, byte[] content, IBasicProperties customProperties);
        void Start();
    }
}