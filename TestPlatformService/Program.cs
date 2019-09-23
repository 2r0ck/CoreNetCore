using CoreNetCore;
using CoreNetCore.Configuration;
using CoreNetCore.Core;
using CoreNetCore.Helpers;
using CoreNetCore.Models;
using CoreNetCore.MQ;
using CoreNetCore.Utils;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace TestPlatformService
{
    public class Program
    {
        private static void Main(string[] args)
        {
            Stream myFile = File.Create("TestPlatformServiceLog.txt");

            TextWriterTraceListener myTextListener = new
               CustomTrace(myFile);
            Trace.Listeners.Add(myTextListener);

            Trace.AutoFlush = true;

            var host = new CoreHostBuilder()
#if DEBUG
                .ConfigureHostConfiguration(conBuilder =>
                {
                    conBuilder.AddInMemoryCollection(new[] { new KeyValuePair<string, string>(HostDefaults.EnvironmentKey, "Development") });
                })
#endif
                .ConfigureAppConfiguration((context, builder) =>
                {
                    builder.AddJsonFile("config.json");
                    Trace.TraceInformation($"Load config file: config.json");
                })
                .ConfigureServices(sc =>
                {
                    sc.AddScoped<IPlatformService, MyService1>();

                    sc.AddScoped<IMessageHandler, MyQueryHanlder>();
                    sc.AddScoped<IMessageHandler, MyResponseHanlder>();
                })
            .Build();

            host.RunAsync().GetAwaiter().GetResult();
        }

        internal class MyService1 : MessageSenderBase, IPlatformService
        {
            private readonly IPrepareConfigService config;

            public MyService1(ICoreDispatcher dispatcher, IPrepareConfigService config) : base(dispatcher)
            {
                this.config = config;
            }

            public Task StartAsync(CancellationToken cancellationToken)
            {
                Trace.TraceInformation("MyService1 is started");
                var serviceName = $"{config.Starter._this._namespace}:{config.Starter._this.servicename}:{config.Starter._this.majorversion}";

                var request1 = CreateMessage().RequestAsync(serviceName,
                     ExchangeTypes.EXCHANGETYPE_FANOUT,
                        "qry:handler",
                        new DataArgs<string>("Test_String"),
                        "resp:handler",
                        "test_context_data",
                        null)
                        .ContinueWith(result =>
                        {
                            if (result.Exception != null)
                            {
                                throw result.Exception;
                            }
                            Console.WriteLine("request1 send successfully");
                        });

                var request2 = CreateMessage().RequestAsync(serviceName,
                   ExchangeTypes.EXCHANGETYPE_FANOUT,
                      "qry:handler",
                      new DataArgs<string>("Test_String2"),
                      (response) =>
                      {
                          var obj = response.FromJson<DataArgs<object>>();
                          if (obj.result == false)
                          {
                              Console.WriteLine("Received response error>>" + obj.error);
                          }
                          else
                          {
                              Trace.TraceInformation($"Received response 2. Data: {response}.");
                          }
                      },
                      null)
                      .ContinueWith(result =>
                      {
                          if (result.Exception != null)
                          {
                              throw result.Exception;
                          }
                          Console.WriteLine("request 2 send successfully");
                      });
                return Task.WhenAll(request1, request2);
            }           
        }

        internal class MyQueryHanlder : QueryHandlerBase
        {
            public override string HandlerName => "qry:handler";

            public override Action<MessageEntry> Handler => Worker;

            public MyQueryHanlder(ICoreDispatcher dispatcher) : base(dispatcher)
            {
            }

            private void Worker(MessageEntry msg)
            {
                Trace.TraceInformation($"Received query. Data: {msg.ReceivedMessage.GetMessageContentString()}");
               // throw new Exception("test exception");
                msg.ResponseOk("MyQueryHanlder - ok!");
            }
        }

        internal class MyResponseHanlder : ResponseHandlerBase
        {
            public override string HandlerName => "resp:handler";

            public override Action<MessageEntry, string> Handler => Worker;

            public MyResponseHanlder(ICoreDispatcher dispatcher) : base(dispatcher)
            {
            }

            private void Worker(MessageEntry msg, string dataContext)
            {
                Trace.TraceInformation($"Received response. Data: {msg.ReceivedMessage.GetMessageContentString()}. DataConext: {dataContext}");
            }
        }
    }
}