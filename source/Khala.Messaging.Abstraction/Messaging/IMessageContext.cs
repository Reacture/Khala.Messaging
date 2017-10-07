namespace Khala.Messaging
{
    using System.Threading.Tasks;

    public interface IMessageContext<TData>
        where TData : class
    {
        TData Data { get; }

        Task Acknowledge();
    }
}
