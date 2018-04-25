namespace Khala.Messaging.Azure
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.EventHubs;

    /// <summary>
    /// Provides the implementation of <see cref="IMessageBus"/> for Azure Event Hubs.
    /// </summary>
    public sealed class EventHubMessageBus : IMessageBus
    {
        private readonly IEventDataSender _eventDataSender;
        private readonly EventDataSerializer _serializer;

        /// <summary>
        /// Initializes a new instance of the <see cref="EventHubMessageBus"/> class with an <see cref="IEventDataSender"/> and an <see cref="EventDataSerializer"/>.
        /// </summary>
        /// <param name="eventDataSender">An <see cref="IEventDataSender"/>.</param>
        /// <param name="serializer">An <see cref="EventDataSerializer"/> to serialize messages.</param>
        public EventHubMessageBus(
            IEventDataSender eventDataSender,
            EventDataSerializer serializer)
        {
            _eventDataSender = eventDataSender ?? throw new ArgumentNullException(nameof(eventDataSender));
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EventHubMessageBus"/> class with an <see cref="EventHubClient"/> and an <see cref="EventDataSerializer"/>.
        /// </summary>
        /// <param name="eventHubClient">An <see cref="EventHubClient"/>.</param>
        /// <param name="serializer">An <see cref="EventDataSerializer"/> to serialize messages.</param>
        public EventHubMessageBus(
            EventHubClient eventHubClient,
            EventDataSerializer serializer)
        {
            _eventDataSender = new EventDataSender(eventHubClient);
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
        }

#pragma warning disable SA1642 // Constructor summary documentation must begin with standard text
        /// <summary>
        /// This constructor is obsolete. Use <see cref="EventHubMessageBus(EventHubClient, EventDataSerializer)"/> instead.
        /// </summary>
        /// <param name="eventHubClient">An <see cref="EventHubClient"/>.</param>
        /// <param name="messageSerializer">An <see cref="IMessageSerializer"/> to serialize messages.</param>
        [Obsolete("Use EventHubMessageBus(EventHubClient, EventDataSerializer) instead. This method will be removed in version 1.0.0.")]
#pragma warning restore SA1642 // Constructor summary documentation must begin with standard text
        public EventHubMessageBus(
            EventHubClient eventHubClient,
            IMessageSerializer messageSerializer)
            : this(eventHubClient, new EventDataSerializer(messageSerializer))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EventHubMessageBus"/> class with an <see cref="EventHubClient"/>.
        /// </summary>
        /// <param name="eventHubClient">An <see cref="EventHubClient"/>.</param>
        public EventHubMessageBus(EventHubClient eventHubClient)
            : this(eventHubClient, new EventDataSerializer())
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

            EventData eventData = _serializer.Serialize(envelope);
            string partitionKey = (envelope.Message as IPartitioned)?.PartitionKey;
            return partitionKey == null
                ? _eventDataSender.Send(new[] { eventData })
                : _eventDataSender.Send(new[] { eventData }, partitionKey);
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

            IEnumerable<EventData> messages =
                from envelope in envelopeList
                select _serializer.Serialize(envelope);

            return partitionKey == null
                ? _eventDataSender.Send(messages)
                : _eventDataSender.Send(messages, partitionKey);
        }
    }
}
