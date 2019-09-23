using System;
using System.Collections.Concurrent;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace CoreNetCore.Utils
{
    public class HttpLocalWorker
    {
        private bool disposeObject;

        private ConcurrentDictionary<string, Action<HttpListenerResponse>> handlers = new ConcurrentDictionary<string, Action<HttpListenerResponse>>();

        private bool shouldExit;
        private ManualResetEvent shouldExitWaitHandle;

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
                listener.IgnoreWriteExceptions = true;
                listener.Prefixes.Add($"http://*:{Port}/");
                listener.Start();
                start?.Invoke();

                while (!shouldExit)
                {
                    var contextAsyncResult = listener.BeginGetContext(
                            (IAsyncResult asyncResult) =>
                            {                              
                                if (listener != null && listener.IsListening)
                                {
                                    var context = listener.EndGetContext(asyncResult);

                                    Action<HttpListenerResponse> handle;
                                    if (handlers.TryGetValue(context.Request.RawUrl, out handle))
                                    {
                                        handle?.Invoke(context.Response);
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

        public void AddGet(string url, Action<HttpListenerResponse> action)
        {
            handlers.AddOrUpdate(url, action, (k, f) => action);
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