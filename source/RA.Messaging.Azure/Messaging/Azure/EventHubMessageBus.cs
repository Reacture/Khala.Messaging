namespace ReactiveArchitecture.Messaging.Azure
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Messaging;
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
            object message,
            CancellationToken cancellationToken)
        {
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            EventData eventData = GetEventData(message);
            return _eventHubClient.SendAsync(eventData);
        }

        public Task SendBatch(
            IEnumerable<object> messages,
            CancellationToken cancellationToken)
        {
            if (messages == null)
            {
                throw new ArgumentNullException(nameof(messages));
            }

            var eventDataList = new List<EventData>();

            foreach (object message in messages)
            {
                if (message == null)
                {
                    throw new ArgumentException(
                        $"{nameof(messages)} cannot contain null.",
                        nameof(messages));
                }

                eventDataList.Add(GetEventData(message));
            }

            return _eventHubClient.SendBatchAsync(eventDataList);
        }

        private EventData GetEventData(object message)
        {
            string json = _serializer.Serialize(message);
            byte[] bytes = Encoding.UTF8.GetBytes(json);
            var eventData = new EventData(bytes);

            var partitioned = message as IPartitioned;
            if (partitioned != null)
            {
                eventData.PartitionKey = partitioned.PartitionKey;
            }

            return eventData;
        }
    }
}
