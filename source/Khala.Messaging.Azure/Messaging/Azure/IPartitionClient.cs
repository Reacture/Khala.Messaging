namespace Khala.Messaging.Azure
{
    using System.Threading.Tasks;
    using Microsoft.Azure.EventHubs;

    public interface IPartitionClient
    {
        Task Checkpoint(EventData eventData);
    }
}
