using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CoreNetCore.Utils
{
    public class HttpLocalWorker  
    {
        bool disposeObject;

        ConcurrentDictionary<string, Func<string>> handlers = new ConcurrentDictionary<string, Func<string>>();

        bool shouldExit;
        ManualResetEvent shouldExitWaitHandle;

        public int Port { get; }

        public HttpLocalWorker(int port)
        {
            shouldExit = false;
            shouldExitWaitHandle = new ManualResetEvent(shouldExit);
            Port = port;
        }

        public async Task StartAsync(Action start = null, Action end = null)
        {
            if (!HttpListener.IsSupported)
            {
                throw new CoreException("HttpListener not supported.");                 
            }

            await Task.Run(() =>
            {
                var listener = new HttpListener();
                listener.Prefixes.Add($"http://*:{Port}/");
                listener.Start();                
                start?.Invoke();

                while (!shouldExit)
                {
                    var contextAsyncResult = listener.BeginGetContext(
                            (IAsyncResult asyncResult) =>
                            {
                                if (listener!=null && listener.IsListening)
                                {
                                    var context = listener.EndGetContext(asyncResult);

                                    Func<string> handle = null;
                                    if(handlers.TryGetValue(context.Request.RawUrl,out handle))
                                    {
                                        using (var writer = new StreamWriter(context.Response.OutputStream))
                                        {
                                            var content = handle?.Invoke() ?? string.Empty;
                                            writer.WriteLine(content);
                                        }
                                    }
                                }
                            }, null);

                    WaitHandle.WaitAny(new WaitHandle[] { contextAsyncResult.AsyncWaitHandle, shouldExitWaitHandle });
                }
                listener.Stop();
                end?.Invoke();
            });
        }
        public void StopAll()
        {
            shouldExit = true;
            shouldExitWaitHandle.Set();
        }

        public void AddGet(string url, Func<string> func)
        {
            handlers.AddOrUpdate(url, func,(k,f)=>func);
        }

        public void Dispose()
        {
            if (!disposeObject)
            {

                disposeObject = true;
            }
        }
    }
}
