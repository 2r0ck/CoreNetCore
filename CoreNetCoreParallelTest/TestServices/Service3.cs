using CoreNetCore;
using CoreNetCore.MQ;
using CoreNetCoreParallelTest.TestServices.CustomService;
using System;
using System.Collections.Generic;
using System.Text;

namespace CoreNetCoreParallelTest.TestServices
{
    public class Service3 : IPlatformService
    {
        public IAppId AppId { get; }
        public ICustomService Cs { get; }

        public Service3(IAppId appId, ICustomService cs)
        {
            Cs = cs;
            AppId = appId;
        }

        public void Run(string[] args)
        {
           if(AppId.CurrentUID != Cs.AppId)
            {
                throw new InvalidProgramException("Services appId not equals!");
            }
        }
    }
}
