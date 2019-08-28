using CoreNetCore.Utils;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;

namespace CoreNetCore.MQ 
{
    public class CoreDispatcher : ICoreDispatcher
    {
        ConcurrentDictionary<string, List<Action<MessageEntry>>> queryHandlers = new ConcurrentDictionary<string, List<Action<MessageEntry>>>();
        ConcurrentDictionary<string, List<Action<MessageEntry, Dictionary<object, object>>>> responceHandlers = new ConcurrentDictionary<string, List<Action<MessageEntry, Dictionary<object, object>>>>();



        //todo: null input
        public bool DeclareQueryHandler(string actionName, Action<MessageEntry> handler)
        {
            if (handler == null)
            {
                throw new CoreException("QueryHandler is null");
            }
            Trace.TraceInformation($"Declare query handler. Name={actionName}");
            List<Action<MessageEntry>> handlers = null;
            if (queryHandlers.TryGetValue(actionName, out handlers))
            {
                handlers.Add(handler);
                return true;
            }
            return queryHandlers.TryAdd(actionName, new List<Action<MessageEntry>> { handler });
        }

        public bool DeclareResponseHandler(string actionName, Action<MessageEntry, Dictionary<object,object>> handler)
        {
            if (handler == null)
            {
                throw new CoreException("ResponseHandler is null");
            }
            Trace.TraceInformation($"Declare response handler. Name={actionName}");
            List<Action<MessageEntry, Dictionary<object, object>>> handlers = null;
            if (responceHandlers.TryGetValue(actionName, out handlers))
            {
                handlers.Add(handler);
                return true;
            }
            return responceHandlers.TryAdd(actionName, new List<Action<MessageEntry, Dictionary<object, object>>> { handler });
        }


    }
}
