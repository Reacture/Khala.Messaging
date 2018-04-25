namespace Khala.Messaging.Azure
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Microsoft.Azure.EventHubs;

    /// <summary>
    /// Represents an event data sender that sends <see cref="EventData"/> to EventHub.
    /// </summary>
    public interface IEventDataSender
    {
        /// <summary>
        /// Send a batch of <see cref="EventData" /> with the same partitionKey to EventHub. All <see cref="EventData" /> with a partitionKey are guaranteed to land on the same partition.
        /// </summary>
        /// <param name="events">A batch of events to send to EventHub</param>
        /// <param name="partitionKey">The partitionKey will be hashed to determine the partitionId to send the <see cref="EventData" /> to. On the Received message this can be accessed at <see cref="EventData.SystemPropertiesCollection.PartitionKey" />.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task Send(IEnumerable<EventData> events, string partitionKey = default);
    }
}
