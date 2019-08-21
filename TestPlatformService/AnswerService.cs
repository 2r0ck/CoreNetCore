using CoreNetCore;
using CoreNetCore.Models;
using CoreNetCore.MQ;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TestPlatformService
{ 
    public class AnswerService1 : IPlatformService
    {
        private const string NoNameMsg = "Name not decalred!";
        private const string NoRecNameMsg = "Reciever name not decalred!";


        private Dictionary<string, string> QuestionAnswerDictionary = new Dictionary<string, string>()
        {
            {"Query1","Answer1"},
            {"Answer1","Query2"},
            {"Query2","Answer2"},
            {"Answer2","Query3"},
            {"Query3","Answer3"}
        };

        AutoResetEvent lockEvent = new AutoResetEvent(false);

        public ICoreConnection CoreConnection { get; }
        public IConfiguration Configuration { get; }
        public IAppId AppId { get; }
        public string Name { get; private set; }
        public string RecName { get; private set; }
        public string SayMessage { get; private set; }

        public AnswerService1(ICoreConnection coreConnection, IConfiguration configuration,IAppId appId)
        {
            CoreConnection = coreConnection;
            Configuration = configuration;
            AppId = appId;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="args"></param>
        /// <remarks>
        /// params:
        /// 1)Current Service Name
        /// 2)Reciever Service name
        /// 
        /// 
        /// </remarks>
        public void Run(string[] args)
        {
            Name = args?[0];
            if (string.IsNullOrEmpty(Name))
            {
                throw new ArgumentNullException(NoNameMsg);
            }
            RecName = args?[1];
            if (string.IsNullOrEmpty(RecName))
            {
                throw new ArgumentNullException(NoRecNameMsg);
            }

            SayMessage = args.Length == 3 ? args[2].ToString() : null; 
            CoreConnection.Connected += CoreConnection_Connected;

            CoreConnection.Start();
            lockEvent.WaitOne();
            CoreConnection.Dispose();
        }

        private void CoreConnection_Connected(string appId)
        {            
                var exchangeName = Name + ".fanout";
                var consumer = new ConsumerParam()
                {
                    ExchangeParam = new ChannelExchangeParam()
                    {
                        Name = exchangeName,
                        Type = ExchangeTypes.EXCHANGETYPE_FANOUT,
                        Durable = true
                    },
                    QueueParam = new ChannelQueueParam()
                    {
                        Name = exchangeName + "." + appId,
                    }
                };

                CoreConnection.Listen(consumer, (msg) =>
                {
                    var str = Encoding.UTF8.GetString(msg.Content);

                    try
                    {
                        msg.Ask();
                    }catch(Exception ex)
                    {

                    }
                    

                    Trace.TraceInformation($"Listen: {str}");
                    if (QuestionAnswerDictionary.ContainsKey(str))
                    {
                        Say(QuestionAnswerDictionary[str]);
                    }
                    lockEvent.Set();
                });

                if (string.IsNullOrEmpty(SayMessage) && QuestionAnswerDictionary.ContainsKey(SayMessage))
                {
                    Say(SayMessage);
                }
            
        }


        private void Say(string msg)
        {
            var prod = new ProducerParam();
            //be ignored
            prod.RoutingKey = AppId.CurrentUID;
            var exchangeName = RecName + ".fanout";
            prod.ExchangeParam = new ChannelExchangeParam()
            {
                Name = exchangeName,
                Type = ExchangeTypes.EXCHANGETYPE_FANOUT,
                Durable = true                
            };
            Trace.TraceInformation($"Say: {msg}");
            CoreConnection.Publish(prod, Encoding.UTF8.GetBytes(msg), null);

        }

        //private void RunAsProducer()
        //{
        //    throw new NotImplementedException();
        //}
    }
}