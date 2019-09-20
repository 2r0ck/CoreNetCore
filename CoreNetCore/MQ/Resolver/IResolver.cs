using CoreNetCore.Models;
using System;
using System.Threading.Tasks;

namespace CoreNetCore.MQ
{
    public interface IResolver
    {
        bool Bind { get; }

        Task<string> Resolve(string service, string type);

        Task<LinkEntry[]> RegisterSelf();

        event Action<string> Started;

        event Action<string> Stopped;

        void RunRefreshCache();
    }
}