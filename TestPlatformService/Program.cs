using CoreNetCore;
using CoreNetCore.Models;
using CoreNetCore.MQ;
using CoreNetCore.Utils;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Diagnostics;
using System.IO;

namespace TestPlatformService
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            Stream myFile = File.Create("TestPlatformServiceLog.txt");

            TextWriterTraceListener myTextListener = new
               CustomTrace(myFile);
            Trace.Listeners.Add(myTextListener);

            Trace.AutoFlush = true;

            var hostBuilder = new CoreHostBuilder();

            var host = hostBuilder
                       .ConfigureServices((builderContext, services) =>
                       {
                           services.AddScoped<IPlatformService, SubscriberFabric>();
                       }
                       )
                       .Build();

            host.DeclareQueryHandler("ping", pingHandler);

            host.DeclareResponseHandler("res:ping", responsePingHandler);

            host.StartAsync().ContinueWith(res =>
            {
                if (res.Exception != null)
                {
                    Console.WriteLine(res.Exception);
                }
                else
                {
                    Console.WriteLine("App STARTED!");
                }

                var data = new MyType1()
                {
                    MyProperty = 1,
                    MyProperty2 = "ping"
                };

                var data_res = new
                {
                    MyProperty = 2,
                    MyProperty2 = "value response"
                };

                //request1
                Console.WriteLine("send request1..");
                host.CreateMessage().RequestAsync(
                    "platserv:appnetcore:1",
                    ExchangeTypes.EXCHANGETYPE_FANOUT,
                    "ping",
                    new DataArgs<MyType1>(data),

                    "res:ping",
                    data_res.ToJson(),

                    null)
                    .ContinueWith(result =>
                {
                    if (result.Exception != null)
                    {
                        Console.WriteLine(result.Exception);
                    }
                    else
                    {
                        Console.WriteLine("request1 send successfully");
                    }
                });


                var data2 = new MyType1()
                {
                    MyProperty = 2,
                    MyProperty2 = "PING"
                };

                //request2
                Console.WriteLine("send request2..");
                host.CreateMessage().RequestAsync(
                    "platserv:appnetcore:1",
                    ExchangeTypes.EXCHANGETYPE_FANOUT,
                    "ping",
                    new DataArgs<MyType1>(data2),
                    (result) =>
                    {
                        Console.WriteLine($"Callback Handler  Data:[{result.ToJson()}]");
                        Console.WriteLine("profit-2");
                    }, null)
                        .ContinueWith(result =>
                        {
                            if (result.Exception != null)
                            {
                                Console.WriteLine(result.Exception);
                            }
                            else
                            {
                                Console.WriteLine("request2 send successfully");
                            }
                        });
            });

            Console.WriteLine("Press key to exit..");
            Console.ReadLine();

            host.StopAsync();
        }

        private static void responsePingHandler(MessageEntry arg1, string arg2)
        {
            Console.WriteLine($"Response Handler by Context:[{arg2.FromJson<MyType1>().ToJson()}]; Data:[{arg1.ReceivedMessage.GetMessageData<DataArgs<string>>().ToJson()}]");
            Console.WriteLine("profit-1");
        }

        private static void pingHandler(MessageEntry obj)
        {
            var data = obj.ReceivedMessage.GetMessageData<DataArgs<MyType1>>();
            Console.WriteLine("Request Handler Data->" + data.ToJson());
             
            obj.ResponseOk(new DataArgs<string>(data.data.MyProperty2+data.data.MyProperty));
        }

        private class MyType1
        {
            public int MyProperty { get; set; }
            public string MyProperty2 { get; set; }
        }
    }
}