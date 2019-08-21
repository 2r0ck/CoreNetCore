using CoreNetCore.Utils;
using System;
using System.Collections.Generic;
using System.Text;

namespace TestPlatformService
{
   public class TestHttpServer
    {
        public void RunHttpServers()
        {
            HttpLocalWorker http = new HttpLocalWorker(8048);
            http.AddGet("/test", () => "test1");
            http.AddGet("/test2", () => "test2");
            http.StartAsync(() => Console.WriteLine("http listen 8048"), () => Console.WriteLine("http Stop 8048"));

            HttpLocalWorker http2 = new HttpLocalWorker(8049);
            http2.AddGet("/test3", () => "test3");
            http2.AddGet("/test4", () => "test4");
            http2.StartAsync(() => Console.WriteLine("http listen 8049"), () => Console.WriteLine("http Stop 8049"));


            Console.WriteLine("Press any key to exit");
            Console.ReadLine();
            http.StopAll();
            http2.StopAll();
            Console.ReadLine();
        }
    }
}
