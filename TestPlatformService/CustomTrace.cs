using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace TestPlatformService
{
    public class CustomTrace : TextWriterTraceListener
    {

        public CustomTrace(Stream stream) : base(stream)
        {

        }

       

        public override void WriteLine(string message)
        {
            base.WriteLine(message);
            Console.WriteLine(message);
        }
        public override void TraceEvent(TraceEventCache eventCache, string source, TraceEventType eventType, int id, string message)
        {
            base.TraceEvent(eventCache, source, eventType, id, message);
        }
    }
}
