using CoreNetCore.MQ;
using Microsoft.Extensions.Configuration;

namespace CoreNetCore
{
    public  interface  IPlatformService
    {       
        void Run(string[] args);
    }
}