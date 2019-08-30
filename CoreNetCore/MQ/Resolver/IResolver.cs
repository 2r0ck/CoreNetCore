namespace CoreNetCore.MQ
{
    public interface IResolver
    {
        bool Bind { get; }
        string Resolve(string service, string type);
    }
}