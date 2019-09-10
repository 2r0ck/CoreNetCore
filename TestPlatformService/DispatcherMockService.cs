using CoreNetCore;
using CoreNetCore.Models;
using CoreNetCore.MQ;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;

namespace TestPlatformService
{
    public class DispatcherMockService : IPlatformService

    {


        public DispatcherMockService(ICoreConnection coreConnection, IConfiguration configuration, IAppId appId)
        {
            CoreConnection = coreConnection;
            Configuration = configuration;
            AppId = appId;
        }

        public ICoreConnection CoreConnection { get; }
        public IConfiguration Configuration { get; }
        public IAppId AppId { get; }
        private AutoResetEvent lockEvent = new AutoResetEvent(false);
        private bool shExit;

        public void Run(string[] args)
        {
          
            CoreConnection.Connected += CoreConnection_Connected;
            InitConsoleEvents();
            CoreConnection.Dispose();
        }

        private void InitConsoleEvents()
        {
            shExit = true;
            while (shExit)
            {
                Console.WriteLine("enter command (exit/say)");
                var read = Console.ReadLine();
                if(read == "exit")
                {
                    shExit = false;
                }
                if (read == "say")
                {

                }
            }
            
        }
 

        private void CoreConnection_Connected(string obj)
        {
            try
            {
                
                var exchangeName =  "TestPlatformService.direct";
                var consumer = new ConsumerParam()
                {
                    ExchangeParam = new ChannelExchangeParam()
                    {
                        Name = exchangeName,
                        Type = ExchangeTypes.EXCHANGETYPE_DIRECT,                        
                    },
                    QueueParam = new ChannelQueueParam()
                    {
                        Name = exchangeName + "." + AppId.CurrentUID,
                        AutoDelete = true
                    }
                   
                };

                CoreConnection.Listen(consumer, (msg) =>
                {
                    var str = Encoding.UTF8.GetString(msg.Content);
                    Console.WriteLine($"Listen (TestPlatformService): {str}");
                    msg.Ack();
                }); 
            }
            catch (Exception ex)
            {
                Trace.TraceError(ex.ToString());
                throw;
            }
        }

        private void Say(string msg)
        {
            try
            {
                var prod = new ProducerParam();
                //be ignored
                prod.RoutingKey = AppId.CurrentUID;
                var rec_exchangeName = AppId.CurrentUID + ".direct";
                prod.ExchangeParam = new ChannelExchangeParam()
                {
                    Name = "TestPlatformService.direct",
                    Type = ExchangeTypes.EXCHANGETYPE_DIRECT,
                };
                Trace.TraceInformation($"Say TestPlatformService.direct: {msg}");
                CoreConnection.Publish(prod, Encoding.UTF8.GetBytes(msg), null);
            }
            catch (Exception ex)
            {
                Trace.TraceError(ex.ToString());
            }
        }
    }
}
