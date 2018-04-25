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
        /// Initializes a new instance of the <see cref="EventHubMessageBus"/> class with an <see cref="IEventDataSender"/>.
        /// </summary>
        /// <param name="eventDataSender">An <see cref="IEventDataSender"/>.</param>
        public EventHubMessageBus(IEventDataSender eventDataSender)
            : this(eventDataSender, new EventDataSerializer())
        {
        }

#pragma warning disable SA1642 // Constructor summary documentation must begin with standard text
        /// <summary>
        /// This constructor is obsolete. Use <see cref="EventHubMessageBus(IEventDataSender, EventDataSerializer)"/> instead.
        /// </summary>
        /// <param name="eventHubClient">An <see cref="EventHubClient"/>.</param>
        /// <param name="serializer">An <see cref="EventDataSerializer"/> to serialize messages.</param>
        [Obsolete("Use EventHubMessageBus(IEventDataSender, EventDataSerializer) instead. This constructor will be removed in version 1.0.0.")]
        public EventHubMessageBus(
            EventHubClient eventHubClient,
            EventDataSerializer serializer)
        {
            _eventDataSender = new EventDataSender(eventHubClient);
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
        }

        /// <summary>
        /// This constructor is obsolete. Use <see cref="EventHubMessageBus(IEventDataSender, EventDataSerializer)"/> instead.
        /// </summary>
        /// <param name="eventHubClient">An <see cref="EventHubClient"/>.</param>
        /// <param name="messageSerializer">An <see cref="IMessageSerializer"/> to serialize messages.</param>
        [Obsolete("Use EventHubMessageBus(IEventDataSender, EventDataSerializer) instead. This constructor will be removed in version 1.0.0.")]
        public EventHubMessageBus(
            EventHubClient eventHubClient,
            IMessageSerializer messageSerializer)
            : this(eventHubClient, new EventDataSerializer(messageSerializer))
        {
        }

        /// <summary>
        /// This constructor is obsolete. Use <see cref="EventHubMessageBus(EventHubClient)"/> instead.
        /// </summary>
        /// <param name="eventHubClient">An <see cref="EventHubClient"/>.</param>
        [Obsolete("Use EventHubMessageBus(IEventDataSender) instead. This constructor will be removed in version 1.0.0.")]
        public EventHubMessageBus(EventHubClient eventHubClient)
            : this(eventHubClient, new EventDataSerializer())
        {
        }
#pragma warning restore SA1642 // Constructor summary documentation must begin with standard text

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
            return _eventDataSender.Send(new[] { eventData }, partitionKey);
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

            IReadOnlyList<Envelope> envelopeList =
                envelopes as IReadOnlyList<Envelope> ?? envelopes.ToList();

            if (envelopeList.Count == 0)
            {
                return Task.CompletedTask;
            }

            for (int i = 0; i < envelopeList.Count; i++)
            {
                Envelope envelope = envelopeList[i];
                if (envelope == null)
                {
                    throw new ArgumentException(
                        $"{nameof(envelopes)} cannot contain null.",
                        nameof(envelopes));
                }
            }

            object firstMessage = envelopeList[0].Message;
            string partitionKey = (firstMessage as IPartitioned)?.PartitionKey;

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

            return _eventDataSender.Send(messages, partitionKey);
        }
    }
}
