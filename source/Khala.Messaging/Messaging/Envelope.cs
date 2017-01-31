namespace Khala.Messaging
{
    using System;

    public class Envelope
    {
        public Envelope(object message)
            : this(Guid.NewGuid(), null, message)
        {
        }

        public Envelope(Guid correlationId, object message)
            : this(Guid.NewGuid(), correlationId, message)
        {
        }

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

        public Guid MessageId { get; }

        public Guid? CorrelationId { get; }

        public object Message { get; }
    }
}
