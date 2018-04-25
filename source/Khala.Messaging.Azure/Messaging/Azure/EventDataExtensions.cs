namespace Khala.Messaging.Azure
{
    using System;
    using Microsoft.Azure.EventHubs;

    /// <summary>
    /// Provides a set of <c>static</c> extension methods for <see cref="EventData"/>.
    /// </summary>
    public static class EventDataExtensions
    {
        /// <summary>
        /// Gets the value of operation id property from event properties.
        /// </summary>
        /// <param name="eventData">An <see cref="EventData"/> that contains properties.</param>
        /// <returns>The value of operation id property if it exists; otherwise, <c>default</c>.</returns>
        public static Guid? GetOperationId(this EventData eventData)
        {
            if (eventData == null)
            {
                throw new ArgumentNullException(nameof(eventData));
            }

            return EventDataSerializer.GetOperationId(eventData.Properties);
        }
    }
}
