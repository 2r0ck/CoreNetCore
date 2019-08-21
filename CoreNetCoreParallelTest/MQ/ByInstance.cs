﻿using CoreNetCore;
using CoreNetCoreParallelTest.TestServices;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Diagnostics;
using System.IO;

namespace CoreNetCoreParallelTest.MQ
{
    [TestClass]
    public class ByInstance
    {
        [TestMethod]
        [DoNotParallelize]
        public void RunInstance3()
        {
            Stream myFile = File.Create("TestFile1.txt");

            /* Create a new text writer using the output stream, and add it to
             * the trace listeners. */
        
           
            var hostBuilder = new CoreHostBuilder();
            var host = hostBuilder.ConfigureAppConfiguration((builderContext, configurationBuilder) => configurationBuilder.AddJsonFile("config1.json", true, true))
                       .ConfigureServices((builderContext, services) => services.AddScoped<IPlatformService, Service1>())
                       .Build();
            hostBuilder.RunPlatformService(null);
            Trace.Flush();
        }

        [TestMethod]
        [DoNotParallelize]
        public void RunInstance4()
        {
            var hostBuilder = new CoreHostBuilder();

            hostBuilder.ConfigureServices((builderContext, services) => services.AddScoped<IPlatformService, Service1>())
                       .Build();

            hostBuilder.RunPlatformService(null);
        }
    }
}