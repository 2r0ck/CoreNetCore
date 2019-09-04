namespace CoreNetCore.Configuration
{
    public interface IPrepareConfigService
    {
        CfgStarterSection Starter { get; }
        CfgMqSection MQ { get;  }
    }
}