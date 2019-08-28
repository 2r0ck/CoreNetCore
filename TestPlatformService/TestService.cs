using CoreNetCore;
using CoreNetCore.MQ;
using System;
using System.Collections.Generic;
using System.Text;

namespace TestPlatformService
{
    public class TestService : IPlatformService
    {
        public IAppId AppId { get; }

        public void Run(string[] args)
        { 
        }
    }
}
