namespace Khala.Messaging
{
    using System;

    /// <summary>
    /// Contains a strongly-typed message object and related properties. Generally this class is used by <see cref="IMessageHandler"/> implementors.
    /// </summary>
    /// <typeparam name="TMessage">The type of the message.</typeparam>
    public sealed class Envelope<TMessage>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Envelope{TMessage}"/> class.
        /// </summary>
        /// <param name="messageId">The identifier of the message.</param>
        /// <param name="message">The strongly-typed message object.</param>
        /// <param name="operationId">The identifier of the operation.</param>
        /// <param name="correlationId">The identifier of the correlation.</param>
        /// <param name="contributor">Information of the contributor to the message.</param>
        public Envelope(Guid messageId, TMessage message, Guid? operationId, Guid? correlationId, string contributor)
        {
            if (messageId == Guid.Empty)
            {
                throw new ArgumentException(
                    $"{nameof(messageId)} cannot be empty.",
                    nameof(messageId));
            }

            if (operationId == Guid.Empty)
            {
                throw new ArgumentException(
                    $"{nameof(operationId)} cannot be empty.",
                    nameof(operationId));
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

        /// <summary>
        /// Gets the identifier of the message.
        /// </summary>
        /// <value>
        /// The identifier of the message.
        /// </value>
        public Guid MessageId { get; }

        /// <summary>
        /// Gets the message object.
        /// </summary>
        /// <value>
        /// The message object.
        /// </value>
        public TMessage Message { get; }

        /// <summary>
        /// Gets the identifier of the message.
        /// </summary>
        /// <value>
        /// The identifier of the message.
        /// </value>
        public Guid? OperationId { get; }

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
    }
}
