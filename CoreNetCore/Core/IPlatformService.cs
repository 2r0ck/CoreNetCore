using System.Threading;
using System.Threading.Tasks;

namespace CoreNetCore
{
    public interface IPlatformService
    {
        Task StartAsync(CancellationToken cancellationToken);
    }
}