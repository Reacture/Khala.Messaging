﻿namespace Khala.Messaging.Azure
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.ServiceBus.Messaging;

    public class EventHubMessageBus : IMessageBus
    {
        private readonly EventDataSerializer _serializer;
        private readonly EventHubClient _eventHubClient;

        public EventHubMessageBus(
            EventDataSerializer serializer,
            EventHubClient eventHubClient)
        {
            if (serializer == null)
            {
                throw new ArgumentNullException(nameof(serializer));
            }

            if (eventHubClient == null)
            {
                throw new ArgumentNullException(nameof(eventHubClient));
            }

            _eventHubClient = eventHubClient;
            _serializer = serializer;
        }

        public EventHubMessageBus(
            IMessageSerializer messageSerializer,
            EventHubClient eventHubClient)
            : this(new EventDataSerializer(messageSerializer), eventHubClient)
        {
        }

        public EventHubMessageBus(EventHubClient eventHubClient)
            : this(new EventDataSerializer(), eventHubClient)
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
            await _eventHubClient.SendAsync(eventData).ConfigureAwait(false);
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
                EventData eventData = await _serializer.Serialize(envelope).ConfigureAwait(false);
                eventDataList.Add(eventData);
            }

            await _eventHubClient.SendBatchAsync(eventDataList).ConfigureAwait(false);
        }
    }
}