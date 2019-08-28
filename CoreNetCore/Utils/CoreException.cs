using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace CoreNetCore
{
    [Serializable]
     public class CoreException :Exception
    {
        public CoreException(string message) :base(message)
        {
            Trace.TraceError(message);
        }

        public CoreException(string message, Exception innerException) : base(message, innerException)
        {
            Trace.TraceError(message);
        }

        public CoreException(Exception targetException) : base(targetException?.Message, targetException)
        {
            Trace.TraceError(targetException?.Message);
        }
    }
}
