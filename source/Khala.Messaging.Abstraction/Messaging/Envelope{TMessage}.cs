namespace Khala.Messaging
{
    using System;

    /// <summary>
    /// Contains a strongly-typed message object and related properties. Generally this class is used by <see cref="IMessageHandler"/> implementors.
    /// </summary>
    /// <typeparam name="TMessage">The type of the message.</typeparam>
    public sealed class Envelope<TMessage>
        where TMessage : class
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Envelope{TMessage}"/> class.
        /// </summary>
        /// <param name="messageId">The identifier of the message.</param>
        /// <param name="correlationId">The identifier of the correlation.</param>
        /// <param name="message">The strongly-typed message object.</param>
        public Envelope(Guid messageId, Guid? correlationId, TMessage message)
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
        /// Gets the message object.
        /// </summary>
        /// <value>
        /// The message object.
        /// </value>
        public TMessage Message { get; }
    }
}
