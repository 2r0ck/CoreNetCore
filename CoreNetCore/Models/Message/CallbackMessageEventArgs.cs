using System;
using System.Collections.Concurrent;

namespace CoreNetCore.Models
{
    public class CallbackMessageEventArgs<T> where T : class
    {
        public bool IsSuccess { get; set; }

        private ConcurrentQueue<Exception> ExceptionQueue;

        public AggregateException AgrException
        {
            get
            {
                if (ExceptionQueue != null && ExceptionQueue.Count > 0)
                {
                    return new AggregateException(ExceptionQueue);
                }
                return null;
            }
        }

        public T Result { get; set; }

        public CallbackMessageEventArgs()
        {
        }

        public CallbackMessageEventArgs(Exception ex)
        {
            var aggregate = ex as AggregateException;
            if (aggregate != null)
            {
                foreach (var e in aggregate.Flatten().InnerExceptions)
                {
                    SetException(e);
                }
            }
            else
            {
                SetException(ex);
            }
            IsSuccess = false;
            Result = null;
        }

        public void SetException(Exception ex)
        {
            if (ExceptionQueue == null)
            {
                ExceptionQueue = new ConcurrentQueue<Exception>();
            }
            ExceptionQueue.Enqueue(ex);
        }
    }
}