﻿using CoreNetCore;
using CoreNetCore.Models;
using CoreNetCore.MQ;
using CoreNetCore.Utils;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace TestPlatformService
{
    internal class Program
    {

        private static  void Main(string[] args)
        {
            //Stream myFile = File.Create("TestPlatformServiceLog.txt");

            //TextWriterTraceListener myTextListener = new
            //   CustomTrace(myFile);
            //Trace.Listeners.Add(myTextListener);
            ////myTextListener.TraceOutputOptions = TraceOptions.DateTime;

            //Trace.AutoFlush = true;

            //var hostBuilder = new CoreHostBuilder();

            //var host = hostBuilder
            //           .ConfigureServices((builderContext, services) =>
            //           {
            //               services.AddScoped<IPlatformService, AnswerService1>();
            //           }
            //           )
            //           .Build();

            //TODO
            //host.DeclareRS();
            //host.DeclareRQ();
            //host.Create();
            //host.StartAsync();


            //hostBuilder.RunPlatformService(new[] { "serviceConsoleApp1", "serviceConsoleApp2", "Query1" });

            //var hel = host.Services.GetService<IHealthcheck>();
            //hel.StartAsync();

            //Console.ReadLine();

            //hel.AddCheck(() => false);
            //Console.WriteLine("Press key to exit..");
            //Console.ReadLine();


            //var hostBuilder = new CoreHostBuilder();
            //var host = hostBuilder.Build();

            //var hs = host.Services.GetService<IHealthcheck>();
            //hs.StartAsync();

            //Console.WriteLine("Press key to exit..");
            //Console.ReadLine();





            //var hostBuilder = new CoreHostBuilder();

            //var host = hostBuilder
            //           .ConfigureServices((builderContext, services) =>
            //           {
            //               services.AddScoped<IPlatformService, TestService>();
            //           }
            //           )
            //           .Build();
            //host.GetService<IPlatformService>().Run();


              NewMethod();

            Console.WriteLine("Press key to exit..");
            Console.ReadLine();

        }

        private static async void  NewMethod()
        {
            try
            {
                ////var t = await Task.Run(() => { throw new Exception("lalal"); return 1; }).ContinueWith(res => { return res.Result; });
                //return Push1().ContinueWith(res => {

                //    if (res.Exception != null)
                //    {

                //    }

                //    return res.Result;

                //});
                var c = await Push1();
                Console.WriteLine(c.Result);

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                
            }

        }



        public static  Task<CallbackMessageEventArgs<object>> Push1()
        {
            //throw new Exception("lalalal3");
            //return await Push2();


            return Push2().ContinueWith((res) =>
            {
                //if (res.Exception != null)
                //{
                //    throw res.Exception;
                //}
                
                // throw new Exception("lalalal3");
                return res.Result;
            });




        }


        public static Task<CallbackMessageEventArgs<object>> Push2()
        {
            //throw new Exception("lalalal3");
            //return await Push2();


            return Push3().ContinueWith((res) =>
            {
                //if (res.Exception != null)
                //{
                //    throw res.Exception;
                //}

                // throw new Exception("lalalal3");
                return res.Result;
            });




        }

        public static Task<CallbackMessageEventArgs<object>> Push3()
        {
            TaskCompletionSource<CallbackMessageEventArgs<object>> tcs = new TaskCompletionSource<CallbackMessageEventArgs<object>>();
            try
            {
                throw new Exception("lalalal2");
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }
           
            return tcs.Task;
        }
    }
}