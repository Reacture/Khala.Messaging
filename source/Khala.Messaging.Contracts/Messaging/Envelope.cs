namespace Khala.Messaging
{
    using System;

    /// <summary>
    /// Contains a message object and related properties.
    /// </summary>
    public sealed class Envelope
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Envelope"/> class with the message object.
        /// </summary>
        /// <param name="message">The message object.</param>
        public Envelope(object message)
            : this(Guid.NewGuid(), default, default, message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Envelope"/> class with information of the contributor to the message and the message object.
        /// </summary>
        /// <param name="contributor">Information of the contributor to the message.</param>
        /// <param name="message">The message object.</param>
        public Envelope(string contributor, object message)
            : this(Guid.NewGuid(), default, contributor, message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Envelope"/> class with the identifier of the correlation and the message object.
        /// </summary>
        /// <param name="correlationId">The identifier of the correlation.</param>
        /// <param name="message">The message object.</param>
        public Envelope(Guid correlationId, object message)
            : this(Guid.NewGuid(), correlationId, default, message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Envelope"/> class with the identifier of the message, the identifier of the correlation and the message object.
        /// </summary>
        /// <param name="messageId">The identifier of the message.</param>
        /// <param name="correlationId">The identifier of the correlation.</param>
        /// <param name="message">The message object.</param>
        public Envelope(Guid messageId, Guid? correlationId, object message)
            : this(messageId, correlationId, default, message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Envelope"/> class with the identifier of the message, the identifier of the correlation, information of the contributor to the message and the message object.
        /// </summary>
        /// <param name="messageId">The identifier of the message.</param>
        /// <param name="correlationId">The identifier of the correlation.</param>
        /// <param name="contributor">Information of the contributor to the message.</param>
        /// <param name="message">The message object.</param>
        public Envelope(Guid messageId, Guid? correlationId, string contributor, object message)
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

            MessageId = messageId;
            CorrelationId = correlationId;
            Contributor = contributor;
            Message = message ?? throw new ArgumentNullException(nameof(message));
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
        /// Gets information of the contributor to the message.
        /// </summary>
        /// <value>
        /// Information of the contributor to the message.
        /// </value>
        public string Contributor { get; }

        /// <summary>
        /// Gets the message object.
        /// </summary>
        /// <value>
        /// The message object.
        /// </value>
        public object Message { get; }
    }
}
