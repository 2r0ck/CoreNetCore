using CoreNetCore.Configuration;
using CoreNetCore.Utils;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace CoreNetCore.MQ
{
    public class Healthcheck : IHealthcheck
    {
        private HttpLocalWorker http;

        private IList<Func<bool>> checks { get; }
        private CfgMqSection ConfigMq { get; }

        public Healthcheck(IPrepareConfigService configuration)
        {
            checks = new List<Func<bool>>();
            ConfigMq = configuration.MQ;
        }

        public async Task StartAsync()
        {
            var port = ConfigMq?.healthcheckPort ?? 8048;
            http = new HttpLocalWorker(port);
            http.AddGet("/healthcheck", (response) =>
            {
                try
                {
                    int statusCode = 500;
                    string result = "false";
                    if (Validate())
                    {
                        statusCode = 200;
                        result = "true";
                    }
                    response.StatusCode = statusCode;
                    using (var writer = new StreamWriter(response.OutputStream))
                    {
                        writer.WriteLine(result);
                    }

                }catch(Exception ex)
                {
                    Trace.TraceError("Healthcheck error!");
                    Trace.TraceError(ex.ToString());
                }

            });

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