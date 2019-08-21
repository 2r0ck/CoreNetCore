using System;
using System.Threading.Tasks;

namespace CoreNetCore.MQ
{
    public interface IHealthcheck
    {
        void AddCheck(Func<bool> check);
        Task StartAsync();

        void Stop();
    }
}