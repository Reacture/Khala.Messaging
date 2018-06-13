namespace Khala.Messaging.Azure
{
    using System.Collections.Generic;

    public class MessageContent
    {
        public MessageContent(Envelope envelope)
            => Envelope = envelope;

        public Envelope Envelope { get; }

        public IDictionary<string, object> Properties { get; } = new Dictionary<string, object>();
    }
}
