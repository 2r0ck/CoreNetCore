using CoreNetCore;
using CoreNetCore.Configuration;
using CoreNetCore.Models;
using CoreNetCore.MQ;
using CoreNetCore.Utils;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

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

            var host = hostBuilder.ConfigureAppConfiguration((context, builder) =>
            {
                builder.AddJsonFile("config.json");
                Trace.TraceInformation($"Load config file: config.json");
            })  
            .Build();
            

            host.DeclareQueryHandler("ping_nc", pingHandler);

            host.DeclareResponseHandler("res:ping_nc", responsePingHandler);

            host.StartAsync().ContinueWith(res =>
            {
                if (res.Exception != null)
                {
                    Console.WriteLine(res.Exception);
                    Environment.Exit(1);
                }
                else
                {
                    Console.WriteLine("App STARTED!");
                }
                #region Send test requests
                bool self_test_requests = true;
                if (self_test_requests)
                {
                    var config = host.GetService<IPrepareConfigService>();

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
                    var selfName = $"{config.Starter._this._namespace}:{config.Starter._this.servicename}:{config.Starter._this.majorversion}";
                    //request1
                    Console.WriteLine("send request1..");
                    host.CreateMessage().RequestAsync(
                        selfName,
                        ExchangeTypes.EXCHANGETYPE_FANOUT,
                        "ping_nc",
                        new DataArgs<MyType1>(data),
                        "res:ping_nc",
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
                    }).Wait();
                }
                //var data2 = new MyType1()
                //{
                //    MyProperty = 2,
                //    MyProperty2 = "PING"
                //};

                ////request2
                //Console.WriteLine("send request2..");
                //host.CreateMessage().RequestAsync(
                //    "platserv:appnetcore:1",
                //    ExchangeTypes.EXCHANGETYPE_FANOUT,
                //    "ping_nc",
                //    new DataArgs<MyType1>(data2),
                //    (result) =>
                //    {
                //        var obj = result.FromJson<DataArgs<object>>();
                //        if (obj.result == false)
                //        {
                //            Console.WriteLine("Error>>" + obj.error);
                //        }
                //        else
                //        {
                //            Console.WriteLine($"Callback Handler  Data:[{result}]");
                //            Console.WriteLine("profit-2");
                //        }
                //    }, new MessageEntryParam() { Timeout = 5000 })
                //        .ContinueWith(result =>
                //        {
                //            if (result.Exception != null)
                //            {
                //                Console.WriteLine(result.Exception);
                //            }
                //            else
                //            {
                //                Console.WriteLine("request2 send successfully");
                //            }
                //        });

                ////request3 to core-js service
                /*Console.WriteLine("send request3 to js service..");
                host.CreateMessage().RequestAsync(
                    "grabb:appjs:1",
                    ExchangeTypes.EXCHANGETYPE_FANOUT,
                    "ping_js",
                    new DataArgs<MyType1>(data2),
                    (result) =>
                    {
                        Console.WriteLine($"Callback Handler  Data:[{result}]");
                        Console.WriteLine("profit-3");
                    }, new MessageEntryParam() { Timeout = 5000 })
                        .ContinueWith(result =>
                        {
                            if (result.Exception != null)
                            {
                                Console.WriteLine(result.Exception);
                            }
                            else
                            {
                                Console.WriteLine("request3 send successfully");
                            }
                        });*/

                #endregion Send Requests
            });

            Console.WriteLine("Press key to exit..");
            Console.ReadLine();

            host.StopAsync();
            Console.ReadLine();
        }

        private static void responsePingHandler(MessageEntry arg1, string arg2)
        {
            try
            {
                Console.WriteLine($"Response Handler by Context:[{arg2.FromJson<MyType1>().ToJson()}]; Data:[{arg1.ReceivedMessage.GetMessageData<DataArgs<string>>().ToJson()}]");               
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        private static void pingHandler(MessageEntry obj)
        {
            try
            {
                /// var data = obj.ReceivedMessage.GetMessageData<DataArgs<MyType1>>();
                // Console.WriteLine("Request Handler Data->" + data.ToJson());

                Console.WriteLine("Request Handler Data->" + obj.ReceivedMessage.GetMessageContentString());
                obj.ResponseOk(new DataArgs<string>("net core answer: hello"));
            }
            catch (Exception ex)
            {
                obj.ResponseError(ex);
            }
        }

        private class MyType1
        {
            public int MyProperty { get; set; }
            public string MyProperty2 { get; set; }
        }

        //private static void Main2(string[] args)
        //{
        //    Stack<char> testStack = new Stack<char>();
        //    testStack.Push('A');
        //    testStack.Push('B');
        //    testStack.Push('C');
        //    testStack.Push('D');
        //    testStack.Push('E');
        //    Console.WriteLine($"testStack->{JsonConvert.SerializeObject(testStack)}");
        //    Enumerable.Range(1, 5).Select(x => { Console.Write($"pop_{x}={testStack.Pop()}; "); return 0; }).ToList();
        //    Console.ReadKey();
        //}

        //private static void Main1(string[] args)
        //{
        //    //"{\"queue\":[{\"replyTo\":\"qu.platserv.appnetcore.1.fanout\",\"appId\":\"757b95b6-451b-42c9-9674-4607275746aa\",\"mqWorkKind\":\"fanout\",\"messageId\":\"3c3be247-3cae-4548-8508-f116160cd3b6\",\"type\":\"ping\",\"priority\":0,\"doResolve\":false,\"handlerMethod\":\"res:ping\",\"context\":\"{\\\"MyProperty\\\":2,\\\"MyProperty2\\\":\\\"value response\\\"}\"}]}"
        //    var via2 = "{\"queue\":[{\"replyTo\":\"qu.platserv.appnetcore.1.fanout\",\"appId\":\"757b95b6-451b-42c9-9674-4607275746aa\",\"mqWorkKind\":\"fanout\",\"messageId\":\"3c3be247-3cae-4548-8508-f116160cd3b6\",\"type\":\"ping\",\"priority\":0,\"doResolve\":false,\"handlerMethod\":\"res:ping\",\"context\":\"{\\\"MyProperty\\\":2,\\\"MyProperty2\\\":\\\"value response\\\"}\"}]}";
        //    var via = "{\"queue\":[{\"replyTo\":\"qu.grabb.appjs.1.fanout\",\"mqWorkKind\":\"fanout\",\"messageId\":\"aa9c6dcc - 4729 - 4670 - 877b - ba269755c81c\",\"type\":\"ping\",\"priority\":0,\"doResolve\":false,\"handlerMethod\":\"rsp: ping1\",\"context\":{\"var1\":\"1\",\"var2\":2,\"var3\":3}}]}";
        //    var via3 = "{\"queue\":[{\"replyTo\":\"qu.grabb.appjs.1.fanout\",\"mqWorkKind\":\"fanout\",\"messageId\":\"aa9c6dcc - 4729 - 4670 - 877b - ba269755c81c\",\"type\":\"ping\",\"priority\":0,\"doResolve\":false,\"handlerMethod\":\"rsp: ping1\"}]}";
        //    var data = via.FromJson<ViaContainer>();
        //    var data2 = via2.FromJson<ViaContainer>();
        //    var data3 = via3.FromJson<ViaContainer>();
        //}
    }
}