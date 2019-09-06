using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace CoreNetCore.Models 
{
    public class PendingEventArgs
    {
        public bool IsSelf { get; set; }

        public bool IsSend { get; set; }

        public bool IsCacheUpdate { get; set; }
        public ResolverEntry Request { get; set; }

        public TaskCompletionSource<LinkEntry[]> Context { get; set; }

    }
}
