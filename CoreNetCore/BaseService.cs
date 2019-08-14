using CoreNetCore.MQ;
using Microsoft.Extensions.Configuration;

namespace CoreNetCore
{
    public abstract class BaseService
    {
        public BaseService(IAppId appId, IConfiguration configuration)
        {
            AppId = appId.CurrentUID;

            Configuration = configuration;
        }

        public string AppId
        {
            get;
        }

        protected IConfiguration Configuration
        {
            get;
        }

        public abstract void Run(string[] args);
    }
}