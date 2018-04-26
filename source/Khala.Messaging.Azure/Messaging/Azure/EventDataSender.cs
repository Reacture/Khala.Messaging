namespace Khala.Messaging.Azure
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.Azure.EventHubs;

    /// <summary>
    /// Provides an event data sender that sends <see cref="EventData"/> to EventHub.
    /// </summary>
    public class EventDataSender
    {
        private readonly EventDataSerializer _serializer;
        private readonly EventHubClient _eventHubClient;

        /// <summary>
        /// Initializes a new instance of the <see cref="EventDataSender"/> class.
        /// </summary>
        /// <param name="eventHubClient">An <see cref="EventHubClient"/>.</param>
        public EventDataSender(EventHubClient eventHubClient)
        {
            _serializer = new EventDataSerializer();
            _eventHubClient = eventHubClient ?? throw new ArgumentNullException(nameof(eventHubClient));
        }

        internal Task Send(
            IEnumerable<Envelope> envelopes, string partitionKey)
        {
            IReadOnlyCollection<EventData> events =
                new List<EventData>(
                    from envelope in envelopes
                    select Serialize(envelope));

            return Send(events, partitionKey);
        }

        /// <summary>
        /// Serializes <see cref="Envelope"/> instance into <see cref="EventData"/>.
        /// </summary>
        /// <param name="envelope"><see cref="Envelope"/> to serialize.</param>
        /// <returns>An <see cref="EventData"/> that contains serialized data.</returns>
        protected virtual EventData Serialize(Envelope envelope)
        {
            return _serializer.Serialize(envelope);
        }

        /// <summary>
        /// Send a batch of <see cref="EventData" /> with the same partitionKey to EventHub. All <see cref="EventData" /> with a partitionKey are guaranteed to land on the same partition.
        /// </summary>
        /// <param name="events">A batch of events to send to EventHub</param>
        /// <param name="partitionKey">The partitionKey will be hashed to determine the partitionId to send the <see cref="EventData" /> to. On the Received message this can be accessed at <see cref="EventData.SystemPropertiesCollection.PartitionKey" />.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        protected virtual Task Send(
            IReadOnlyCollection<EventData> events, string partitionKey)
        {
            return _eventHubClient.SendAsync(events, partitionKey);
        }
    }
}
