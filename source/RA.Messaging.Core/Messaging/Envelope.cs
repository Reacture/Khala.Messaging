namespace ReactiveArchitecture.Messaging
{
    using System;
    using Newtonsoft.Json;

    public class Envelope
    {
        public Envelope(object message)
        {
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            MessageId = Guid.NewGuid();
            CorrelationId = null;
            Message = message;
        }

        public Envelope(Guid correlationId, object message)
        {
            if (correlationId == Guid.Empty)
            {
                throw new ArgumentException(
                    $"{nameof(correlationId)} cannot be empty.",
                    nameof(correlationId));
            }

            MessageId = Guid.NewGuid();
            CorrelationId = correlationId;
            Message = message;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "This constructor is used by json deserializing. Refer to the following URL for more information: http://www.newtonsoft.com/json/help/html/SerializationAttributes.htm")]
        [JsonConstructor]
        private Envelope(Guid messageId, Guid? correlationId, object message)
        {
            MessageId = messageId;
            CorrelationId = correlationId;
            Message = message;
        }

        public Guid MessageId { get; }

        public Guid? CorrelationId { get; }

        public object Message { get; }
    }
}
