using System.Threading.Tasks;

namespace CoreNetCore.MQ
{
    public interface IResolver
    {
        bool Bind { get; }
        Task<string> Resolve(string service, string type);
    }
}