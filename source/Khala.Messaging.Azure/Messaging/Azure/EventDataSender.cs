namespace Khala.Messaging.Azure
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Microsoft.Azure.EventHubs;

    /// <summary>
    /// Provides the default implementation of <see cref="IEventDataSender"/>.
    /// </summary>
    public class EventDataSender : IEventDataSender
    {
        private readonly EventHubClient _eventHubClient;

        /// <summary>
        /// Initializes a new instance of the <see cref="EventDataSender"/> class.
        /// </summary>
        /// <param name="eventHubClient">An <see cref="EventHubClient"/>.</param>
        public EventDataSender(EventHubClient eventHubClient)
        {
            _eventHubClient = eventHubClient ?? throw new ArgumentNullException(nameof(eventHubClient));
        }

        /// <inheritdoc/>
        public Task Send(IEnumerable<EventData> events)
        {
            if (events == null)
            {
                throw new ArgumentNullException(nameof(events));
            }

            var eventDataList = new List<EventData>();

            foreach (EventData eventData in events)
            {
                if (eventData == null)
                {
                    throw new ArgumentException(
                        $"{nameof(events)} cannot contain null.",
                        nameof(events));
                }

                eventDataList.Add(eventData);
            }

            if (eventDataList.Count == 0)
            {
                return Task.CompletedTask;
            }

            return _eventHubClient.SendAsync(eventDataList);
        }

        /// <inheritdoc/>
        public Task Send(IEnumerable<EventData> events, string partitionKey)
        {
            if (events == null)
            {
                throw new ArgumentNullException(nameof(events));
            }

            if (partitionKey == null)
            {
                throw new ArgumentNullException(nameof(partitionKey));
            }

            var eventDataList = new List<EventData>();

            foreach (EventData eventData in events)
            {
                if (eventData == null)
                {
                    throw new ArgumentException(
                        $"{nameof(events)} cannot contain null.",
                        nameof(events));
                }

                eventDataList.Add(eventData);
            }

            if (eventDataList.Count == 0)
            {
                return Task.CompletedTask;
            }

            return _eventHubClient.SendAsync(eventDataList, partitionKey);
        }
    }
}
