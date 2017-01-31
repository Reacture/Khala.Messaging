namespace Khala.Messaging.Azure
{
    using System;
    using System.IO;
    using System.Text;
    using System.Threading.Tasks;
    using Microsoft.ServiceBus.Messaging;

    /// <summary>
    /// Serializes and deserializes <see cref="Envelope"/> objects into and from <see cref="BrokeredMessage"/>.
    /// </summary>
    public class BrokeredMessageSerializer
    {
        private readonly IMessageSerializer _messageSerializer;

        /// <summary>
        /// Initializes a new instance of the <see cref="BrokeredMessageSerializer"/> class with an <see cref="IMessageSerializer"/>.
        /// </summary>
        /// <param name="messageSerializer"><see cref="IMessageSerializer"/> to serialize enveloped messages.</param>
        public BrokeredMessageSerializer(IMessageSerializer messageSerializer)
        {
            if (messageSerializer == null)
            {
                throw new ArgumentNullException(nameof(messageSerializer));
            }

            _messageSerializer = messageSerializer;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BrokeredMessageSerializer"/> class.
        /// </summary>
        public BrokeredMessageSerializer()
            : this(new JsonMessageSerializer())
        {
        }

        /// <summary>
        /// Serializes <see cref="Envelope"/> instance into <see cref="BrokeredMessage"/>.
        /// </summary>
        /// <param name="envelope"><see cref="Envelope"/> to serialize.</param>
        /// <returns>A task representing the asynchronous operation. The task result contains an <see cref="BrokeredMessage"/> that contains serialized data.</returns>
        public Task<BrokeredMessage> Serialize(Envelope envelope)
        {
            if (envelope == null)
            {
                throw new ArgumentNullException(nameof(envelope));
            }

            object message = envelope.Message;

            string value = _messageSerializer.Serialize(message);
            byte[] body = Encoding.UTF8.GetBytes(value);

            var messageId = envelope.MessageId.ToString("n");
            var correlationId = envelope.CorrelationId?.ToString("n");

            return Task.FromResult(new BrokeredMessage(new MemoryStream(body))
            {
                MessageId = messageId,
                CorrelationId = correlationId,
                Properties =
                {
                    ["Khala.Envelope.MessageId"] = messageId,
                    ["Khala.Envelope.CorrelationId"] = correlationId
                },
                PartitionKey = (message as IPartitioned)?.PartitionKey
            });
        }

        /// <summary>
        /// Deserializes <see cref="Envelope"/> from <see cref="BrokeredMessage"/>.
        /// </summary>
        /// <param name="brokeredMessage"><see cref="BrokeredMessage"/> that contains serialized data.</param>
        /// <returns>A task representing the asynchronous operation. The task result contains an <see cref="Envelope"/> instance deserialized.</returns>
        public Task<Envelope> Deserialize(BrokeredMessage brokeredMessage)
        {
            if (brokeredMessage == null)
            {
                throw new ArgumentNullException(nameof(brokeredMessage));
            }

            return DeserializeEnvelope(brokeredMessage);
        }

        private static Guid? ParseGuid(object property)
        {
            Guid value;
            return Guid.TryParse(property?.ToString(), out value)
                ? value
                : default(Guid?);
        }

        private async Task<Envelope> DeserializeEnvelope(BrokeredMessage brokeredMessage)
        {
            object messageId;
            brokeredMessage.Properties.TryGetValue(
                "Khala.Envelope.MessageId", out messageId);

            object correlationId;
            brokeredMessage.Properties.TryGetValue(
                "Khala.Envelope.CorrelationId", out correlationId);

            using (var stream = brokeredMessage.GetBody<Stream>())
            using (var reader = new StreamReader(stream, Encoding.UTF8))
            {
                string value = await reader.ReadToEndAsync().ConfigureAwait(false);
                object message = _messageSerializer.Deserialize(value);

                return new Envelope(
                    ParseGuid(messageId) ?? Guid.NewGuid(),
                    ParseGuid(correlationId),
                    message);
            }
        }
    }
}
