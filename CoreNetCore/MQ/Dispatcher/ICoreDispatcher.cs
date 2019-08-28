using System;
using System.Collections.Generic;

namespace CoreNetCore.MQ
{ 
    public interface ICoreDispatcher
    {
        bool DeclareQueryHandler(string actionName, Action<MessageEntry> handler);
        bool DeclareResponseHandler(string actionName, Action<MessageEntry, Dictionary<object, object>> handler);
    }
}