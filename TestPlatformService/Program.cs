using CoreNetCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Diagnostics;

namespace TestPlatformService
{
    class Program
    {
        static void Main(string[] args)
        {

            Trace.TraceInformation("test");

            var hostBuilder = new CoreHostBuilder();

            var host = hostBuilder
                       .ConfigureServices((builderContext, services) => {
                           services.AddScoped<IPlatformService, AnswerService1>();

                       }
                       )
                       .Build();

            hostBuilder.RunPlatformService(new[] { "service1", "service2", "Query1" });


        }
    }
}
