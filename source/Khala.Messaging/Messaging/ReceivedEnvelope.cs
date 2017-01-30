namespace Khala.Messaging
{
    using System;

    public class ReceivedEnvelope<TMessage>
    {
        public ReceivedEnvelope(
            Guid messageId,
            Guid? correlationId,
            TMessage message)
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

        public Guid MessageId { get; }

        public Guid? CorrelationId { get; }

        public TMessage Message { get; }
    }
}
