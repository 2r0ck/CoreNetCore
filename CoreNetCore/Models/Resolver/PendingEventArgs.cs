using System;
using System.Collections.Generic;
using System.Text;

namespace CoreNetCore.Models 
{
    public class PendingEventArgs
    {
        public bool IsSelf { get; set; }

        public bool IsSend { get; set; }

        public ResolverEntry Request { get; set; }

    }
}
