using CoreNetCore;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace TestPlatformService
{
    public class SubscriberFabric : IPlatformService 
    {

        public SubscriberFabric(IMemoryCache  memoryCache)
        {
            MemoryCache = memoryCache;
        }

        private ConcurrentDictionary<string, TaskCompletionSource<string>> pending = new ConcurrentDictionary<string, TaskCompletionSource<string>>();

        public IMemoryCache MemoryCache { get; }

        public async Task<string> GetValue(string key,int index)
        {
            var cache = GetCacheValue(key);
            if (cache == null)
            {
                var tsc = pending.GetOrAdd(key, new TaskCompletionSource<string>(key));
                var result = await tsc.Task;
                return index + ":" + result;
            }
            else
            {
                return "cache:" + cache;
            }
        }

        public string GetCacheValue(string key)
        {
            object value = null;
            MemoryCache.TryGetValue(key, out value);
            return value?.ToString();
        }

        public void AddCacheValue(string key, string value)
        {
            MemoryCache.Set(key, value);            
        }



        public string RunService(string key)
        {
            Console.WriteLine($"Run service for key {key}");
            Thread.Sleep(5000);
            Console.WriteLine($"Key {key} return.");
            return Guid.NewGuid().ToString();
        }

        public void Test()
        {
            var key = "abc";
            GetValue(key,1).ContinueWith(res => { if (res.Exception != null) { Console.WriteLine(res.Exception.Message); } Console.WriteLine($"Task return -> {res.Result}"); });
            GetValue(key,2).ContinueWith(res => { if (res.Exception != null) { Console.WriteLine(res.Exception.Message); } Console.WriteLine($"Task return -> {res.Result}"); });      
            GetValue(key,3).ContinueWith(res => { if (res.Exception != null) { Console.WriteLine(res.Exception.Message); } Console.WriteLine($"Task return -> {res.Result}"); });         
            GetValue(key,4).ContinueWith(res => { if (res.Exception != null) { Console.WriteLine(res.Exception.Message); } Console.WriteLine($"Task return -> {res.Result}"); });       
            GetValue(key,5).ContinueWith(res => { if (res.Exception != null) { Console.WriteLine(res.Exception.Message); } Console.WriteLine($"Task return -> {res.Result}"); });
            
            push();

            Task.Run(() =>
            {
                Thread.Sleep(1000);
                return GetValue(key, 6);
            }).ContinueWith(res => { if (res.Exception != null) { Console.WriteLine(res.Exception.Message); } Console.WriteLine($"Task return -> {res.Result}"); });
            push();
        }

        public void push()
        {
            Task.Run(() =>
            {
                Console.WriteLine($"push start");
                foreach (var element_key in pending.Keys)
                {                    
                    TaskCompletionSource<string> completionSource;
                    if (pending.TryRemove(element_key, out completionSource))
                    {
                        var obj = completionSource.Task.AsyncState;
                        var result = Guid.NewGuid().ToString() + $" objstate:[{obj}]";
                        AddCacheValue(element_key, result);
                      completionSource.SetResult(result);
                    }
                }
            });
        }

        public void Run(string[] args = null)
        {
            Test();
        }
    }

    public class SubscriberFabric3
    {
        private ConcurrentDictionary<string, ConcurrentQueue<Action<string[]>>> pending = new ConcurrentDictionary<string, ConcurrentQueue<Action<string[]>>>();

        public async Task<string> GetValue(string key)
        {
            return await Task.Run(() =>
             {
                 string result = string.Empty;
                 var syncPrinitive = new AutoResetEvent(false);
                 var queue = pending.GetOrAdd(key, new ConcurrentQueue<Action<string[]>>());
                 queue.Enqueue((string[] links) =>
                 {
                     result = $"{key}:{links[0]}";
                     syncPrinitive.Set();
                 });
                 syncPrinitive.WaitOne();
                 return result;
             });
        }

        public string RunService(string key)
        {
            Console.WriteLine($"Run service for key {key}");
            Thread.Sleep(5000);
            Console.WriteLine($"Key {key} return.");
            return Guid.NewGuid().ToString();
        }

        public void Test()
        {
            var key = "abc";
            GetValue(key).ContinueWith(res => { if (res.Exception != null) { Console.WriteLine(res.Exception.Message); } Console.WriteLine($"Task return -> {res.Result}"); });
            push();
            GetValue(key).ContinueWith(res => { if (res.Exception != null) { Console.WriteLine(res.Exception.Message); } Console.WriteLine($"Task return -> {res.Result}"); });
            push();
            GetValue(key).ContinueWith(res => { if (res.Exception != null) { Console.WriteLine(res.Exception.Message); } Console.WriteLine($"Task return -> {res.Result}"); });
            push();
            GetValue(key).ContinueWith(res => { if (res.Exception != null) { Console.WriteLine(res.Exception.Message); } Console.WriteLine($"Task return -> {res.Result}"); });
            push();
            GetValue(key).ContinueWith(res => { if (res.Exception != null) { Console.WriteLine(res.Exception.Message); } Console.WriteLine($"Task return -> {res.Result}"); });
            push();
        }

        public void push()
        {
            Task.Run(() =>
            {
                Console.WriteLine($"push start");
                foreach (var element_key in pending.Keys)
                {
                    ConcurrentQueue<Action<string[]>> queue;
                    if (pending.TryRemove(element_key, out queue))
                    {
                        string[] parameters = { Guid.NewGuid().ToString() };
                        Action<string[]> action;
                        while (queue.TryDequeue(out action))
                        {
                            action.Invoke(parameters);
                        }
                    }
                }
            });
        }
    }

    public class SubscriberFabric2
    {
        private Dictionary<string, Task<string>> pending = new Dictionary<string, Task<string>>();

        public async Task<string> GetValue(string key)
        {
            if (AddPendingElement(key))
            {
                pending[key].Start();
            }
            return await pending[key];
        }

        private bool AddPendingElement(string key)
        {
            lock (pending)
            {
                if (!pending.ContainsKey(key))
                {
                    pending.Add(key, new Task<string>(() => RunService(key)));
                    return true;
                }
            }
            return false;
        }

        public string RunService(string key)
        {
            Console.WriteLine($"Run service for key {key}");
            Thread.Sleep(5000);
            Console.WriteLine($"Key {key} return.");
            return $"result({key})";
        }

        public void Test()
        {
            Task.Run(() => GetValue("abc").ContinueWith(res => { if (res.Exception != null) { Console.WriteLine(res.Exception.Message); } Console.WriteLine($"Task return -> {res.Result}"); }));
            Task.Run(() => GetValue("abc").ContinueWith(res => { if (res.Exception != null) { Console.WriteLine(res.Exception.Message); } Console.WriteLine($"Task return -> {res.Result}"); }));
            Task.Run(() => GetValue("abc").ContinueWith(res => { if (res.Exception != null) { Console.WriteLine(res.Exception.Message); } Console.WriteLine($"Task return -> {res.Result}"); }));
            Task.Run(() => GetValue("abc").ContinueWith(res => { if (res.Exception != null) { Console.WriteLine(res.Exception.Message); } Console.WriteLine($"Task return -> {res.Result}"); }));
            Task.Run(() => GetValue("abc").ContinueWith(res => { if (res.Exception != null) { Console.WriteLine(res.Exception.Message); } Console.WriteLine($"Task return -> {res.Result}"); }));

            Task.Run(() =>
            {
                Thread.Sleep(8000);
                Console.WriteLine($"Run again..");
                return GetValue("abc");
            })
            .ContinueWith(res =>
            {
                if (res.Exception != null)
                {
                    Console.WriteLine(res.Exception.Message);
                }
                Console.WriteLine($"Task return -> {res.Result}");
            });
        }
    }

    public class SubscriberFabric0
    {
        private ConcurrentDictionary<string, Task<string>> pending = new ConcurrentDictionary<string, Task<string>>();

        public async Task<string> GetValue(string key)
        {
            return await pending.GetOrAdd(key, new Task<string>(() => RunService(key)));
        }

        public string RunService(string key)
        {
            Console.WriteLine($"Run service for key {key}");
            Thread.Sleep(5000);
            Console.WriteLine($"Key {key} return.");
            return $"result({key})";
        }

        public void Test()
        {
            var key = "abc";
            Task.Run(() => GetValue(key).ContinueWith(res => { if (res.Exception != null) { Console.WriteLine(res.Exception.Message); } Console.WriteLine($"Task return -> {res.Result}"); }));
            Task.Run(() => GetValue(key).ContinueWith(res => { if (res.Exception != null) { Console.WriteLine(res.Exception.Message); } Console.WriteLine($"Task return -> {res.Result}"); }));
            Task.Run(() => GetValue(key).ContinueWith(res => { if (res.Exception != null) { Console.WriteLine(res.Exception.Message); } Console.WriteLine($"Task return -> {res.Result}"); }));
            Task.Run(() => GetValue(key).ContinueWith(res => { if (res.Exception != null) { Console.WriteLine(res.Exception.Message); } Console.WriteLine($"Task return -> {res.Result}"); }));
            Task.Run(() => GetValue(key).ContinueWith(res => { if (res.Exception != null) { Console.WriteLine(res.Exception.Message); } Console.WriteLine($"Task return -> {res.Result}"); }));

            Task.Run(() =>
            {
                Thread.Sleep(8000);
                Console.WriteLine($"Run again..");
                return GetValue(key);
            })
            .ContinueWith(res =>
            {
                if (res.Exception != null)
                {
                    Console.WriteLine(res.Exception.Message);
                }
                Console.WriteLine($"Task return -> {res.Result}");
            });

            Thread.Sleep(1000);
            pending[key].Start();
        }
    }
}