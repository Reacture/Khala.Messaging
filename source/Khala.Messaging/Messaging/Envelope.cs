namespace Khala.Messaging
{
    using System;

    /// <summary>
    /// Contains a message object and related properties.
    /// </summary>
    public class Envelope
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Envelope"/> class with the message object.
        /// </summary>
        /// <param name="message">The message object.</param>
        public Envelope(object message)
            : this(Guid.NewGuid(), null, message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Envelope"/> class with the identifier of the correlation and the message object.
        /// </summary>
        /// <param name="correlationId">The identifier of the correlation.</param>
        /// <param name="message">The message object.</param>
        public Envelope(Guid correlationId, object message)
            : this(Guid.NewGuid(), correlationId, message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Envelope"/> class with the identifier of the message, the identifier of the correlation and the message object.
        /// </summary>
        /// <param name="messageId">The identifier of the message.</param>
        /// <param name="correlationId">The identifier of the correlation.</param>
        /// <param name="message">The message object.</param>
        public Envelope(Guid messageId, Guid? correlationId, object message)
        {
            if (messageId == Guid.Empty)
            {
                throw new ArgumentException(
                    $"{nameof(messageId)} cannot be empty.",
                    nameof(messageId));
            }

            if (correlationId == Guid.Empty)
            {
                throw new ArgumentException(
                    $"{nameof(correlationId)} cannot be empty.",
                    nameof(correlationId));
            }

            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            MessageId = messageId;
            CorrelationId = correlationId;
            Message = message;
        }

        /// <summary>
        /// Gets the identifier of the message.
        /// </summary>
        /// <value>
        /// The identifier of the message.
        /// </value>
        public Guid MessageId { get; }

        /// <summary>
        /// Gets the identifier of the correlation.
        /// </summary>
        /// <value>
        /// The identifier of the correlation.
        /// </value>
        public Guid? CorrelationId { get; }

        /// <summary>
        /// Gets the message object.
        /// </summary>
        /// <value>
        /// The message object.
        /// </value>
        public object Message { get; }
    }
}
