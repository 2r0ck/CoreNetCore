using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Linq;

namespace CoreNetCore.Models
{
    public class DataArgs<T> where T : class
    {
        public bool result { get; set; }

        public string error => ExceptionQueue != null ? string.Join("; ", ExceptionQueue.Select(x => x.Message)) : null;

        [JsonIgnore]
        private ConcurrentQueue<Exception> ExceptionQueue;
     

        public T data { get; set; }

        public DataArgs()
        {
           
        }

        public DataArgs(T _data)
        {
            this.data = _data;
            result = true;
        }

        public DataArgs(Exception ex)
        {
            SetException(ex);
            result = false;
            data = null;
        }

        public void SetException(Exception ex)
        {
            result = false;
            var aggregate = ex as AggregateException;
            if (aggregate != null)
            {
                foreach (var e in aggregate.Flatten().InnerExceptions)
                {
                    AddNewException(e);
                }
            }
            else
            {
                AddNewException(ex);
            }
        }

        private void AddNewException(Exception ex)
        {
            if (ExceptionQueue == null)
            {
                ExceptionQueue = new ConcurrentQueue<Exception>();
            }
            ExceptionQueue.Enqueue(ex);
        }
    }
}