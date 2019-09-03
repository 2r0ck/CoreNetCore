using RabbitMQ.Client;

namespace CoreNetCore.Models
{
    public sealed class ExchangeTypes
    {
        /// <summary>
        /// Тип прямого обмена
        /// </summary>
        public const string EXCHANGETYPE_DIRECT = ExchangeType.Direct;

        /// <summary>
        /// Тип обмена Fanout направляет сообщения во все связанные очереди без разбора.
        /// </summary>
        public const string EXCHANGETYPE_FANOUT = ExchangeType.Fanout;

        /// <summary>
        /// Тип обмена Тема направляет сообщения в очереди, ключ маршрутизации которых совпадает со всеми или частью ключа маршрутизации.
        /// </summary>
        public const string EXCHANGETYPE_TOPIC  = ExchangeType.Topic;
        /// <summary>
        /// Тип обмена заголовками направляет сообщения на основе сопоставления заголовков сообщений с ожидаемыми заголовками, указанными в очереди привязки.
        /// </summary>
        public const string EXCHANGETYPE_HEADERS = ExchangeType.Headers;

        public static string Get(string str,bool assert =false)
        {
            switch (str.Trim().ToLowerInvariant())
            {
                case EXCHANGETYPE_DIRECT: return EXCHANGETYPE_DIRECT;
                case EXCHANGETYPE_FANOUT: return EXCHANGETYPE_FANOUT;
                case EXCHANGETYPE_TOPIC: return EXCHANGETYPE_TOPIC;
                case EXCHANGETYPE_HEADERS: return EXCHANGETYPE_HEADERS;
                default:
                    {
                        if (assert)
                        {
                            throw new CoreException($"Unknow exchange type {str}");
                        }
                        return null;
                    }
            }

        }

        public static string GetExchangeName(string link, string linkType, string exchangeType)
        {
            if (string.IsNullOrEmpty(linkType))
            {
                return $"{link}.{ExchangeTypes.Get(exchangeType)}";
            }
            else
            {
                return $"{link}.{LinkTypes.GetExchangeTypeByLink(linkType)}";
            }
        }

        private ExchangeTypes()
        {
        }
    }
}