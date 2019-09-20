using CoreNetCore;
using CoreNetCore.MQ;
using CoreNetCoreParallelTest.TestServices.CustomService;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

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

        public Task StartAsync(CancellationToken cancellationToken)
        {
            if (AppId.CurrentUID != Cs.AppId)
            {
                throw new InvalidProgramException("Services appId not equals!");
            }
           return Task.CompletedTask;
        }
    }
}
