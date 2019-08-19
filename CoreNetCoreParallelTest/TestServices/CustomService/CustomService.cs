using CoreNetCore.MQ;
using System;
using System.Collections.Generic;
using System.Text;

namespace CoreNetCoreParallelTest.TestServices.CustomService
{
    public class CustomService : ICustomService

    {
        public string AppId { get; }

        public CustomService(IAppId appId)
        {
            AppId = appId.CurrentUID;
        }
    }
}
