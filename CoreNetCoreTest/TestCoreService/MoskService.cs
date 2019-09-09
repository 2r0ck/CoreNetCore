using CoreNetCore;
using CoreNetCore.Models;
using CoreNetCore.MQ;
using CoreNetCore.Utils;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CoreNetCoreTest.TestCoreService
{
    [TestClass]
    public class MoskService
    {
        private TaskCompletionSource<bool> handlerSet = new TaskCompletionSource<bool>();
        private TaskCompletionSource<bool> querySet = new TaskCompletionSource<bool>();
        private TaskCompletionSource<bool> callbackSet = new TaskCompletionSource<bool>();

        public bool _resultTest;
        object lobj = new object();
        public bool resultTest
        {
            get
            {
                lock (lobj)
                {
                    return _resultTest;
                }
            }

            private set
            {
                lock (lobj)
                {
                    _resultTest = value;
                }
            }
        }      

        [TestMethod]
        //НЕОБХОДИМ ЗАПУЩЕННЫЙ OPERATOR!
        public void MyService()
        {
            var timeoutPoint = Task.Delay(30000).ContinueWith(res => { Trace.TraceWarning("test timeout (30c)"); resultTest = false; });

            var successPoints = Task.WhenAll(querySet.Task, handlerSet.Task, callbackSet.Task).ContinueWith(res =>
            {
                var results = res.Result;
                Trace.TraceInformation("successPoints done");
                resultTest = results.All(x => x);
            });
     

            var hostBuilder = new CoreHostBuilder();
            var host = hostBuilder.Build();

            host.DeclareQueryHandler("ping_nc", pingHandler);

            host.DeclareResponseHandler("res:ping_nc", responsePingHandler);

            host.StartAsync().ContinueWith(res =>
            {
               if (res.Exception != null)
                {
                    Trace.WriteLine(res.Exception);
                    
                }
                else
                {
                    Trace.WriteLine("App STARTED!");
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
                Trace.WriteLine("send request1..");
                host.CreateMessage().RequestAsync(
                    "platserv:appnetcore:1",
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
                            Trace.WriteLine(result.Exception);
                        }
                        else
                        {
                            Trace.WriteLine("request1 send successfully");
                        }
                    }).Wait();

                var data2 = new MyType1()
                {
                    MyProperty = 2,
                    MyProperty2 = "PING"
                };

                //request2
                Trace.WriteLine("send request2..");
                host.CreateMessage().RequestAsync(
                    "platserv:appnetcore:1",
                    ExchangeTypes.EXCHANGETYPE_FANOUT,
                    "ping_nc",
                    new DataArgs<MyType1>(data2),
                    (result) =>
                    {
                        var obj = result.FromJson<DataArgs<object>>();
                        if (obj.result == false)
                        {
                            Trace.WriteLine("Error>>" + obj.error);
                        }
                        else
                        {
                            Trace.WriteLine($"Callback Handler  Data:[{result}]");
                            callbackSet.SetResult(true);
                        }
                    }, new MessageEntryParam() { Timeout = 5000 })
                        .ContinueWith(result =>
                        {
                            if (result.Exception != null)
                            {
                                Trace.WriteLine(result.Exception);
                            }
                            else
                            {
                                Trace.WriteLine("request2 send successfully");
                            }
                        }); 
            });
          

            Task.WaitAny(timeoutPoint, successPoints);

            Assert.IsTrue(resultTest);
        }

        private void responsePingHandler(MessageEntry arg1, string arg2)
        {
            Trace.WriteLine($"Response Handler by Context:[{arg2.FromJson<MyType1>().ToJson()}]; Data:[{arg1.ReceivedMessage.GetMessageData<DataArgs<string>>().ToJson()}]");
            handlerSet.SetResult(true);
        }

        private void pingHandler(MessageEntry obj)
        {
            try
            {
                var data = obj.ReceivedMessage.GetMessageData<DataArgs<MyType1>>();
                Trace.WriteLine("Request Handler Data->" + data.ToJson());

                obj.ResponseOk(new DataArgs<string>(" net core answer:" + data.data.MyProperty2 + data.data.MyProperty));
                querySet.SetResult(true);
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
    }
}