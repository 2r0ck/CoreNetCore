using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Text;

namespace CoreNetCore.Configuration
{
    public class PrepareConfigService : IPrepareConfigService
    {
        public IConfiguration Configuration { get; }
        public CfgStarterSection Starter { get; private set; }

        public CfgMqSection MQ { get; private set; }

        public PrepareConfigService(IConfiguration configuration)
        {
            Configuration = configuration;
            ReadConfig(configuration);
        }

        //ADD MQ и переделать connection
        private void ReadConfig(IConfiguration configuration)
        {
            Starter = new CfgStarterSection();
            configuration.GetSection("starter").Bind(Starter, options => options.BindNonPublicProperties = true);
            Starter.ValidateAndTrace("starter");

            MQ = new CfgMqSection();
            configuration.GetSection("mq").Bind(MQ);
            Starter.ValidateAndTrace("mq");
        }

    }
}
