using CoreNetCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Diagnostics;
using System.IO;

namespace TestPlatformService
{
    class Program
    {
        static void Main(string[] args)
        {

            Stream myFile = File.Create("TestPlatformServiceLog.txt");

            TextWriterTraceListener myTextListener = new
               TextWriterTraceListener(myFile);
            Trace.Listeners.Add(myTextListener);
            Trace.AutoFlush = true;


            var hostBuilder = new CoreHostBuilder();

            var host = hostBuilder
                       .ConfigureServices((builderContext, services) => {
                           services.AddScoped<IPlatformService, AnswerService1>();

                       }
                       )
                       .Build();

            hostBuilder.RunPlatformService(new[] { "serviceConsoleApp1", "serviceConsoleApp2", "Query1" });

            
        }
    }
}
