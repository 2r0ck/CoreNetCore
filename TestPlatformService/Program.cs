using CoreNetCore;
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

        private static void Main(string[] args)
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

            ////hostBuilder.RunPlatformService(new[] { "serviceConsoleApp1", "serviceConsoleApp2", "Query1" });

            //var hel = host.Services.GetService<IHealthcheck>();
            //hel.StartAsync();

            //Console.ReadLine();

            //hel.AddCheck(() => false);
            //Console.WriteLine("Press key to exit..");
            //Console.ReadLine();


            var hostBuilder = new CoreHostBuilder();
            var host = hostBuilder.Build();

            var hs = host.Services.GetService<IHealthcheck>();
            hs.StartAsync();

            Console.WriteLine("Press key to exit..");
            Console.ReadLine();

            Console.WriteLine("Press key to exit..");
            Console.ReadLine();

        }

    }
}