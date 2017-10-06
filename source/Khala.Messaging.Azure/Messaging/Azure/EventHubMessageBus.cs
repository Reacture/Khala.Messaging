namespace Khala.Messaging.Azure
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.EventHubs;

    /// <summary>
    /// Provides the implementation of <see cref="IMessageBus"/> for Azure Event hubs.
    /// </summary>
    public sealed class EventHubMessageBus : IMessageBus
    {
        private readonly EventDataSerializer _serializer;
        private readonly EventHubClient _eventHubClient;

        public EventHubMessageBus(
            EventHubClient eventHubClient,
            EventDataSerializer serializer)
        {
            _eventHubClient = eventHubClient ?? throw new ArgumentNullException(nameof(eventHubClient));
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
        }

        public EventHubMessageBus(
            EventHubClient eventHubClient,
            IMessageSerializer messageSerializer)
            : this(eventHubClient, new EventDataSerializer(messageSerializer))
        {
        }

        public EventHubMessageBus(EventHubClient eventHubClient)
            : this(eventHubClient, new EventDataSerializer())
        {
        }

        public Task Send(
            Envelope envelope,
            CancellationToken cancellationToken)
        {
            if (envelope == null)
            {
                throw new ArgumentNullException(nameof(envelope));
            }

            return SendMessage(envelope);
        }

        private async Task SendMessage(Envelope envelope)
        {
            EventData eventData = await _serializer.Serialize(envelope).ConfigureAwait(false);
            string partitionKey = (envelope.Message as IPartitioned)?.PartitionKey;
            await _eventHubClient.SendAsync(eventData, partitionKey).ConfigureAwait(false);
        }

        public Task SendBatch(
            IEnumerable<Envelope> envelopes,
            CancellationToken cancellationToken)
        {
            if (envelopes == null)
            {
                throw new ArgumentNullException(nameof(envelopes));
            }

            var envelopeList = new List<Envelope>();

            foreach (Envelope envelope in envelopes)
            {
                if (envelope == null)
                {
                    throw new ArgumentException(
                        $"{nameof(envelopes)} cannot contain null.",
                        nameof(envelopes));
                }

                envelopeList.Add(envelope);
            }

            // TODO: Ensure envelopes is not empty.
            // TODO: Ensure same partition keys.
            return SendMessages(envelopeList);
        }

        private async Task SendMessages(IEnumerable<Envelope> envelopes)
        {
            var eventDataList = new List<EventData>();

            foreach (Envelope envelope in envelopes)
            {
                EventData eventData = await _serializer.Serialize(envelope).ConfigureAwait(false);
                eventDataList.Add(eventData);
            }

            await _eventHubClient.SendAsync(eventDataList).ConfigureAwait(false);
        }
    }
}
