namespace CoreNetCore.Models
{
    public sealed class ExchangeTypes
    {
        /// <summary>
        /// Тип прямого обмена
        /// </summary>
        public const string EXCHANGETYPE_DIRECT = "direct";

        /// <summary>
        /// Тип обмена Fanout направляет сообщения во все связанные очереди без разбора.
        /// </summary>
        public const string EXCHANGETYPE_FANOUT = "fanout";

        /// <summary>
        /// Тип обмена Тема направляет сообщения в очереди, ключ маршрутизации которых совпадает со всеми или частью ключа маршрутизации.
        /// </summary>
        public const string EXCHANGETYPE_TOPIC = "topic";

        /// <summary>
        /// Тип обмена заголовками направляет сообщения на основе сопоставления заголовков сообщений с ожидаемыми заголовками, указанными в очереди привязки.
        /// </summary>
        public const string EXCHANGETYPE_HEADERS = "headers";

        private ExchangeTypes()
        {
        }
    }
}