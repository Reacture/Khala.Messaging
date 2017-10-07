namespace Khala.Messaging
{
    using System.Threading.Tasks;

    public interface IMessageDataSerializer<TData>
        where TData : class
    {
        Task<TData> Serialize(Envelope envelope);

        Task<Envelope> Deserialize(TData data);
    }
}
