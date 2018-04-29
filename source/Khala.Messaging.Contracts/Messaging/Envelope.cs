namespace Khala.Messaging
{
    using System;

    /// <summary>
    /// Contains a message object and related properties.
    /// </summary>
    public sealed class Envelope : IEnvelope
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Envelope"/> class with the identifier of the message, the identifier of the operation, the identifier of the correlation, information of the contributor to the message and the message object.
        /// </summary>
        /// <param name="messageId">The identifier of the message.</param>
        /// <param name="message">The message object.</param>
        /// <param name="operationId">The identifier of the operation.</param>
        /// <param name="correlationId">The identifier of the correlation.</param>
        /// <param name="contributor">Information of the contributor to the message.</param>
        public Envelope(
            Guid messageId,
            object message,
            string operationId = default,
            Guid? correlationId = default,
            string contributor = default)
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
            Message = message ?? throw new ArgumentNullException(nameof(message));
            OperationId = operationId;
            CorrelationId = correlationId;
            Contributor = contributor;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Envelope"/> class with the message object.
        /// </summary>
        /// <param name="message">The message object.</param>
        /// <param name="operationId">The identifier of the operation.</param>
        /// <param name="correlationId">The identifier of the correlation.</param>
        /// <param name="contributor">Information of the contributor to the message.</param>
        public Envelope(
            object message,
            string operationId = default,
            Guid? correlationId = default,
            string contributor = default)
            : this(messageId: Guid.NewGuid(), message, operationId, correlationId, contributor)
        {
        }

        /// <inheritdoc/>
        public Guid MessageId { get; }

        /// <summary>
        /// Gets the message object.
        /// </summary>
        /// <value>
        /// The message object.
        /// </value>
        public object Message { get; }

        /// <inheritdoc/>
        public string OperationId { get; }

        /// <inheritdoc/>
        public Guid? CorrelationId { get; }

        /// <inheritdoc/>
        public string Contributor { get; }
    }
}
