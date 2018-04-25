namespace Khala.Messaging.Azure
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
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
        public Task Send(
            IEnumerable<EventData> events, string partitionKey = default)
        {
            if (events == null)
            {
                throw new ArgumentNullException(nameof(events));
            }

            IReadOnlyList<EventData> eventDataList =
                events as IReadOnlyList<EventData> ?? events.ToList();

            if (eventDataList.Count == 0)
            {
                return Task.CompletedTask;
            }

            for (int i = 0; i < eventDataList.Count; i++)
            {
                EventData eventData = eventDataList[i];
                if (eventData == null)
                {
                    throw new ArgumentException(
                        $"{nameof(events)} cannot contain null.",
                        nameof(events));
                }
            }

            return _eventHubClient.SendAsync(eventDataList, partitionKey);
        }
    }
}
