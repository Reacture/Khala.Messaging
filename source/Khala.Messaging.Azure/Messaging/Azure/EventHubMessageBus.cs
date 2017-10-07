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
        private readonly EventHubMessageSerializer _serializer;
        private readonly EventHubClient _eventHubClient;

        public EventHubMessageBus(
            EventHubClient eventHubClient,
            EventHubMessageSerializer serializer)
        {
            _eventHubClient = eventHubClient ?? throw new ArgumentNullException(nameof(eventHubClient));
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
        }

        public EventHubMessageBus(
            EventHubClient eventHubClient,
            IMessageSerializer messageSerializer)
            : this(eventHubClient, new EventHubMessageSerializer(messageSerializer))
        {
        }

        public EventHubMessageBus(EventHubClient eventHubClient)
            : this(eventHubClient, new EventHubMessageSerializer())
        {
        }

        /// <summary>
        /// Sends a single enveloped message to event hub.
        /// </summary>
        /// <param name="envelope">An enveloped message to be sent.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public Task Send(
            Envelope envelope,
            CancellationToken cancellationToken)
        {
            if (envelope == null)
            {
                throw new ArgumentNullException(nameof(envelope));
            }

            return RunSend(envelope);
        }

        private async Task RunSend(Envelope envelope)
        {
            EventData eventData = await _serializer.Serialize(envelope).ConfigureAwait(false);
            string partitionKey = (envelope.Message as IPartitioned)?.PartitionKey;
            await _eventHubClient.SendAsync(eventData, partitionKey).ConfigureAwait(false);
        }

        /// <summary>
        /// Sends multiple enveloped messages to event hub sequentially and atomically.
        /// </summary>
        /// <param name="envelopes">A seqeunce contains enveloped messages.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public Task Send(
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

            if (envelopeList.Count == 0)
            {
                return Task.CompletedTask;
            }

            string partitionKey = (envelopeList[0].Message as IPartitioned)?.PartitionKey;

            for (int i = 1; i < envelopeList.Count; i++)
            {
                Envelope envelope = envelopeList[i];
                if ((envelope.Message as IPartitioned)?.PartitionKey != partitionKey)
                {
                    throw new ArgumentException(
                        "All messages should have same parition key.",
                        nameof(envelopes));
                }
            }

            return RunSend(envelopeList, partitionKey);
        }

        private async Task RunSend(IEnumerable<Envelope> envelopes, string partitionKey)
        {
            var messages = new List<EventData>();

            foreach (Envelope envelope in envelopes)
            {
                messages.Add(await _serializer.Serialize(envelope).ConfigureAwait(false));
            }

            await _eventHubClient.SendAsync(messages, partitionKey).ConfigureAwait(false);
        }
    }
}
