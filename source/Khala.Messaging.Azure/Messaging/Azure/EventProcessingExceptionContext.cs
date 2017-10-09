namespace Khala.Messaging.Azure
{
    using System;
    using Microsoft.Azure.EventHubs;

    public class EventProcessingExceptionContext
    {
        public EventProcessingExceptionContext(EventData eventData, Exception exception)
        {
            EventData = eventData ?? throw new ArgumentNullException(nameof(eventData));
            Exception = exception ?? throw new ArgumentNullException(nameof(exception));
        }

        public EventProcessingExceptionContext(EventData eventData, Envelope envelope, Exception exception)
            : this(eventData, exception)
        {
            Envelope = envelope ?? throw new ArgumentNullException(nameof(envelope));
        }

        public EventData EventData { get; }

        public Envelope Envelope { get; }

        public Exception Exception { get; }
    }
}
