using CoreNetCore;
using CoreNetCore.Configuration;
using CoreNetCore.Core;
using CoreNetCore.Helpers;
using CoreNetCore.Models;
using CoreNetCore.MQ;
using CoreNetCore.Utils;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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
        //были успешно отправлены 2 запроса (один с ответом в хендлер, другой в колбек)
        private static TaskCompletionSource<bool> receiveHandlerSet1 = new TaskCompletionSource<bool>();

        private static TaskCompletionSource<bool> receiveHandlerSet2 = new TaskCompletionSource<bool>();

        //были обработаны 2 запроса
        private static TaskCompletionSource<bool> querySet1 = new TaskCompletionSource<bool>();

        private static TaskCompletionSource<bool> querySet2 = new TaskCompletionSource<bool>();

        //получен ответ в хендлер
        private static TaskCompletionSource<bool> responseHandlerSet = new TaskCompletionSource<bool>();

        //получен ответ в колбек
        private static TaskCompletionSource<bool> callbackSet = new TaskCompletionSource<bool>();

        public bool _resultTest;
        private object lobj = new object();

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
        public async Task MyService()
        {
            var timeoutPoint = Task.Delay(60000).ContinueWith(res => { Trace.TraceWarning("test timeout (1 min)"); return new bool[] { false }; });

            var successPoints = Task.WhenAll(
               receiveHandlerSet1.Task,
               receiveHandlerSet2.Task,
               querySet1.Task,
               querySet2.Task,
               responseHandlerSet.Task,
               callbackSet.Task
                ).ContinueWith(reslt => { Trace.TraceInformation("successPoints done"); return reslt.Result; });

            var host = new CoreHostBuilder()
                .ConfigureServices(sc =>
                {
                    sc.AddScoped<IPlatformService, MyTestService>();

                    sc.AddScoped<IMessageHandler, MyQueryHanlder>();
                    sc.AddScoped<IMessageHandler, MyResponseHanlder>();
                }).Build();

            await host.StartAsync();
            var resTask = await Task.WhenAny(successPoints, timeoutPoint);
            resultTest = resTask.Result.All(x => x);
            Assert.IsTrue(resultTest);
        }

        private class MyTestService : MessageSenderBase, IPlatformService
        {
            private readonly IPrepareConfigService config;

            public MyTestService(ICoreDispatcher dispatcher, IPrepareConfigService config) : base(dispatcher)
            {
                this.config = config;
            }

            public Task StartAsync(CancellationToken cancellationToken)
            {
                Trace.TraceInformation("MyTestService is started");
                var serviceName = $"{config.Starter._this._namespace}:{config.Starter._this.servicename}:{config.Starter._this.majorversion}";

                var request1 = CreateMessage().RequestAsync(serviceName,
                     ExchangeTypes.EXCHANGETYPE_FANOUT,
                        "queryHandler",
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
                            if (!receiveHandlerSet1.Task.IsCompleted)
                                receiveHandlerSet1.SetResult(true);
                            Console.WriteLine("request1 send successfully");
                        });

                var request2 = CreateMessage().RequestAsync(serviceName,
                   ExchangeTypes.EXCHANGETYPE_FANOUT,
                      "queryHandler",
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
                              if (!callbackSet.Task.IsCompleted)
                                  callbackSet.SetResult(true);
                          }
                      },
                      null)
                      .ContinueWith(result =>
                      {
                          if (result.Exception != null)
                          {
                              throw result.Exception;
                          }
                          if (!receiveHandlerSet2.Task.IsCompleted)
                              receiveHandlerSet2.SetResult(true);
                          Console.WriteLine("request 2 send successfully");
                      });
                return Task.WhenAll(request1, request2);
            }
        }

        private class MyQueryHanlder : QueryHandlerBase
        {
            public override string HandlerName => "queryHandler";

            public override Action<MessageEntry> Handler => Worker;

            public MyQueryHanlder(ICoreDispatcher dispatcher) : base(dispatcher)
            {
            }

            private void Worker(MessageEntry msg)
            {
                Trace.TraceInformation($"Received query. Data: {msg.ReceivedMessage.GetMessageContentString()}");
                // throw new Exception("test exception");
                msg.ResponseOk("MyQueryHanlder - ok!");
                if (!string.IsNullOrEmpty(msg.ReceivedMessage.GetVia().GetLast().responseHandlerName))
                {
                    if (!querySet1.Task.IsCompleted)
                        querySet1.SetResult(true); //by handler
                }
                else
                {
                    if (!querySet2.Task.IsCompleted)
                        querySet2.SetResult(true); //by callback
                }
            }
        }

        private class MyResponseHanlder : ResponseHandlerBase
        {
            public override string HandlerName => "resp:handler";

            public override Action<MessageEntry, string> Handler => Worker;

            public MyResponseHanlder(ICoreDispatcher dispatcher) : base(dispatcher)
            {
            }

            private void Worker(MessageEntry msg, string dataContext)
            {
                Trace.TraceInformation($"Received response. Data: {msg.ReceivedMessage.GetMessageContentString()}. DataConext: {dataContext}");
                if (!responseHandlerSet.Task.IsCompleted)
                    responseHandlerSet.SetResult(true);
            }
        }
    }
}