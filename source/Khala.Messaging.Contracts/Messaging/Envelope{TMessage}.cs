namespace Khala.Messaging
{
    using System;

    /// <summary>
    /// Contains a strongly-typed message object and related properties.
    /// </summary>
    /// <typeparam name="TMessage">The type of the message.</typeparam>
    public sealed class Envelope<TMessage> : IEnvelope
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Envelope{TMessage}"/> class.
        /// </summary>
        /// <param name="messageId">The identifier of the message.</param>
        /// <param name="message">The strongly-typed message object.</param>
        /// <param name="operationId">The identifier of the operation.</param>
        /// <param name="correlationId">The identifier of the correlation.</param>
        /// <param name="contributor">Information of the contributor to the message.</param>
        public Envelope(
            Guid messageId,
            TMessage message,
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

#pragma warning disable IDE0016 // Ignore "Use 'throw' expression" because TMessage does not have a reference type constraint.
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }
#pragma warning restore IDE0016 // Ignore "Use 'throw' expression" because TMessage does not have a reference type constraint.

            MessageId = messageId;
            Message = message;
            OperationId = operationId;
            CorrelationId = correlationId;
            Contributor = contributor;
        }

        /// <inheritdoc/>
        public Guid MessageId { get; }

        /// <summary>
        /// Gets the message object.
        /// </summary>
        /// <value>
        /// The message object.
        /// </value>
        public TMessage Message { get; }

        /// <inheritdoc/>
        public string OperationId { get; }

        /// <inheritdoc/>
        public Guid? CorrelationId { get; }

        /// <inheritdoc/>
        public string Contributor { get; }
    }
}
