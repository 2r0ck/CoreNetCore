using CoreNetCore.Models;
using System.Threading.Tasks;

namespace CoreNetCore.MQ
{
    //TODO: Delete this?
    public interface IMessageEntry
    {
        Task RequestAsync(string serviceName, string exchangeType, string queryName, object queryData, MessageEntryParam parameters);
    }
}