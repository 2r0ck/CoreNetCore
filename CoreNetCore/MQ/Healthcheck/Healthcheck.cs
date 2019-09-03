﻿using CoreNetCore.Utils;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace CoreNetCore.MQ
{
    public class Healthcheck : IHealthcheck
    {
        private HttpLocalWorker http;

        private IList<Func<bool>> checks { get; }
        private IConfiguration Configuration { get; }

        public Healthcheck(IConfiguration configuration)
        {
            checks = new List<Func<bool>>();
            Configuration = configuration;

        }

        public async Task StartAsync()
        {
            var port = Configuration.GetIntValue("mq:healthcheckPort") ?? 8048;
            http = new HttpLocalWorker(port);
            http.AddGet("/healthcheck", () => Validate().ToString());

            await http.StartAsync(
                () => Trace.TraceInformation($"Healtcheck started. Port: {port}"),
                () => Trace.TraceInformation($"Healtcheck stoped."));
        }

        void IHealthcheck.AddCheck(Func<bool> check)
        {
            if (check == null)
                throw new CoreException("Healthcheck handler is null");
            checks.Add(check);
        }

        private bool Validate()
        {
            foreach (var check in checks)
            {
                if (!check())
                {
                    return false;
                }
            }
            return true;
        }

        public void Stop()
        {
            http?.StopAll();
        }
    }
}