namespace CoreNetCore.Models
{
    public sealed class LinkTypes
    {
        public const string LINK_EXCHANGE = "exchange";

        public const string LINK_QUEUE = "queue";


        public static string GetLinkByExchangeType(string kind)
        {
            return ExchangeTypes.Get(kind) == ExchangeTypes.EXCHANGETYPE_DIRECT ? LINK_EXCHANGE : LINK_QUEUE;
        }

        public static string GetExchangeTypeByLink(string link)
        {
            return link == LINK_EXCHANGE ? ExchangeTypes.EXCHANGETYPE_DIRECT : ExchangeTypes.EXCHANGETYPE_FANOUT;
        }


        private LinkTypes()
        {
        }
    }
}