namespace Khala.Messaging.Azure
{
    using System.Threading.Tasks;

    public interface IMessageDataSerializer<T>
    {
        Task<T> Serialize(Envelope envelope);

        Task<Envelope> Deserialize(T data);
    }
}
