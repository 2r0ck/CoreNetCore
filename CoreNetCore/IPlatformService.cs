using CoreNetCore.MQ;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;

namespace CoreNetCore
{
    public  interface  IPlatformService
    {       
        void  Run(string[] args=null); 
    }
}