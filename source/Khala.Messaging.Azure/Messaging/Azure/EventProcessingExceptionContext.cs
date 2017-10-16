namespace Khala.Messaging.Azure
{
    using System;
    using Microsoft.Azure.EventHubs;

    /// <summary>
    /// Encapsulates information related to an error occured while processing event data.
    /// </summary>
    public class EventProcessingExceptionContext
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EventProcessingExceptionContext"/> class with an <see cref="EventData"/> and an <see cref="Exception"/>.
        /// </summary>
        /// <param name="eventData">An <see cref="EventData"/> object.</param>
        /// <param name="exception">An <see cref="Exception"/> thrown.</param>
        public EventProcessingExceptionContext(EventData eventData, Exception exception)
        {
            EventData = eventData ?? throw new ArgumentNullException(nameof(eventData));
            Exception = exception ?? throw new ArgumentNullException(nameof(exception));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EventProcessingExceptionContext"/> class with an <see cref="EventData"/>, an <see cref="Envelope"/> and an <see cref="Exception"/>.
        /// </summary>
        /// <param name="eventData">An <see cref="EventData"/> object.</param>
        /// <param name="envelope">An <see cref="Envelope"/> object.</param>
        /// <param name="exception">An <see cref="Exception"/> thrown.</param>
        public EventProcessingExceptionContext(EventData eventData, Envelope envelope, Exception exception)
            : this(eventData, exception)
        {
            Envelope = envelope ?? throw new ArgumentNullException(nameof(envelope));
        }

        /// <summary>
        /// Gets the source <see cref="EventData"/> object.
        /// </summary>
        /// <value>
        /// The source <see cref="EventData"/> object.
        /// </value>
        public EventData EventData { get; }

        /// <summary>
        /// Gets the deserialized <see cref="Envelope"/> object.
        /// </summary>
        /// <value>
        /// The deserialized <see cref="Envelope"/> object.
        /// </value>
        /// <remarks>
        /// The value is <c>null</c> if the exception is thrown before or while deserializing.
        /// </remarks>
        public Envelope Envelope { get; }

        /// <summary>
        /// Gets the <see cref="Exception"/> thrown.
        /// </summary>
        /// <value>
        /// The <see cref="Exception"/> thrown.
        /// </value>
        public Exception Exception { get; }
    }
}
