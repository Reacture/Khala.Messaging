namespace Khala.Messaging.Azure
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.ServiceBus.Messaging;

    public class EventHubMessageBus : IMessageBus
    {
        private readonly EventHubClient _eventHubClient;
        private readonly EventDataSerializer _serializer;

        public EventHubMessageBus(
            EventHubClient eventHubClient, IMessageSerializer messageSerializer)
        {
            if (eventHubClient == null)
            {
                throw new ArgumentNullException(nameof(eventHubClient));
            }

            if (messageSerializer == null)
            {
                throw new ArgumentNullException(nameof(messageSerializer));
            }

            _eventHubClient = eventHubClient;
            _serializer = new EventDataSerializer(messageSerializer);
        }

        public EventDataSerializer Serializer => _serializer;

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
            EventData eventData = await _serializer.Serialize(envelope);
            await _eventHubClient.SendAsync(eventData);
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

            return SendMessages(envelopeList);
        }

        private async Task SendMessages(IEnumerable<Envelope> envelopes)
        {
            var eventDataList = new List<EventData>();

            foreach (Envelope envelope in envelopes)
            {
                eventDataList.Add(await _serializer.Serialize(envelope));
            }

            await _eventHubClient.SendBatchAsync(eventDataList);
        }
    }
}
