using CoreNetCore.MQ;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using System.Threading;
using System.Threading.Tasks;

namespace CoreNetCore
{
    public  interface  IPlatformService 
    {
        //
        // Сводка:
        //     Triggered when the application host is started
        //
        // Параметры:
        //   cancellationToken:
        //     Indicates that the start process has been aborted.
        Task StartAsync(CancellationToken cancellationToken);
        //
        // Сводка:
        //     Triggered when the application host is performing a graceful shutdown.
        //
        // Параметры:
        //   cancellationToken:
        //     Indicates that the shutdown process should no longer be graceful.
        Task StopAsync(CancellationToken cancellationToken);
    }
}