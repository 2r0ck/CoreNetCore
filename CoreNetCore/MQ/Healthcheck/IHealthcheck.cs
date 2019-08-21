using System;

namespace CoreNetCore.MQ
{
    public interface IHealthcheck
    {
        void AddCheck(Func<bool> check);
        void StartAsync();
    }
}