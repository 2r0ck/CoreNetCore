using System;
using System.Collections.Generic;
using System.Text;

namespace CoreNetCore.Models 
{
    public class ResolverInvoker
    {
        public Func<Dictionary<string, string>,string> Progress { get; set; }
        public Action<string> SuccessCallback { get; set; }
        public Action<string> FailCallback { get; set; }
    }

}
