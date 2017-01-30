namespace Khala.Messaging.Azure
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.ServiceBus.Messaging;

    public class EventHubMessageBus : IMessageBus
    {
        private readonly EventHubClient _eventHubClient;
        private readonly IMessageSerializer _serializer;

        public EventHubMessageBus(
            EventHubClient eventHubClient, IMessageSerializer serializer)
        {
            if (eventHubClient == null)
            {
                throw new ArgumentNullException(nameof(eventHubClient));
            }

            if (serializer == null)
            {
                throw new ArgumentNullException(nameof(serializer));
            }

            _eventHubClient = eventHubClient;
            _serializer = serializer;
        }

        public Task Send(
            Envelope envelope,
            CancellationToken cancellationToken)
        {
            if (envelope == null)
            {
                throw new ArgumentNullException(nameof(envelope));
            }

            EventData eventData = GetEventData(envelope);
            return _eventHubClient.SendAsync(eventData);
        }

        public Task SendBatch(
            IEnumerable<Envelope> envelopes,
            CancellationToken cancellationToken)
        {
            if (envelopes == null)
            {
                throw new ArgumentNullException(nameof(envelopes));
            }

            var eventDataList = new List<EventData>();

            foreach (Envelope envelope in envelopes)
            {
                if (envelope == null)
                {
                    throw new ArgumentException(
                        $"{nameof(envelopes)} cannot contain null.",
                        nameof(envelopes));
                }

                eventDataList.Add(GetEventData(envelope));
            }

            return _eventHubClient.SendBatchAsync(eventDataList);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "GetEventData() returns an instance of EventData.")]
        private EventData GetEventData(Envelope envelope)
        {
            string data = _serializer.Serialize(envelope);
            byte[] bytes = Encoding.UTF8.GetBytes(data);
            var eventData = new EventData(bytes);

            var partitioned = envelope.Message as IPartitioned;
            if (partitioned != null)
            {
                eventData.PartitionKey = partitioned.PartitionKey;
            }

            return eventData;
        }
    }
}
