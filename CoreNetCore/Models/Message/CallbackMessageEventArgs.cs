using System;
using System.Collections.Generic;
using System.Text;

namespace CoreNetCore.Models 
{
    public class CallbackMessageEventArgs
    {
        public bool IsSuccess { get; set; }

        public byte ResultCode { get; set; }

        public string ErrorMessage { get; set; }

        public object Result { get; set; }
    }
}
