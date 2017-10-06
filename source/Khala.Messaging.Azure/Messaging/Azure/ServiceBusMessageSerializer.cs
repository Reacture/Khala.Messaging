namespace Khala.Messaging.Azure
{
    using System;
    using System.Text;
    using System.Threading.Tasks;
    using Microsoft.Azure.ServiceBus;

    /// <summary>
    /// Serializes and deserializes <see cref="Envelope"/> objects into and from <see cref="Message"/>.
    /// </summary>
    public sealed class ServiceBusMessageSerializer : IMessageDataSerializer<Message>
    {
        private const string MessageIdName = "Khala.Messaging.Envelope.MessageId";
        private const string CorrelationIdName = "Khala.Messaging.Envelope.CorrelationId";

        private readonly IMessageSerializer _messageSerializer;

        /// <summary>
        /// Initializes a new instance of the <see cref="ServiceBusMessageSerializer"/> class with an <see cref="IMessageSerializer"/>.
        /// </summary>
        /// <param name="messageSerializer"><see cref="IMessageSerializer"/> to serialize enveloped messages.</param>
        public ServiceBusMessageSerializer(IMessageSerializer messageSerializer)
        {
            _messageSerializer = messageSerializer ?? throw new ArgumentNullException(nameof(messageSerializer));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ServiceBusMessageSerializer"/> class.
        /// </summary>
        public ServiceBusMessageSerializer()
            : this(new JsonMessageSerializer())
        {
        }

        private static Guid? ParseGuid(object property)
            => Guid.TryParse(property?.ToString(), out Guid value)
            ? value
            : default(Guid?);

        /// <summary>
        /// Serializes <see cref="Envelope"/> instance into <see cref="Message"/>.
        /// </summary>
        /// <param name="envelope"><see cref="Envelope"/> to serialize.</param>
        /// <returns>A task representing the asynchronous operation. The task result contains an <see cref="Message"/> that contains serialized data.</returns>
        public Task<Message> Serialize(Envelope envelope)
        {
            if (envelope == null)
            {
                throw new ArgumentNullException(nameof(envelope));
            }

            object message = envelope.Message;
            byte[] body = SerializeMessage(message);
            var messageId = envelope.MessageId.ToString("n");
            var correlationId = envelope.CorrelationId?.ToString("n");

            return Task.FromResult(new Message(body)
            {
                MessageId = messageId,
                CorrelationId = correlationId,
                UserProperties =
                {
                    [MessageIdName] = messageId,
                    [CorrelationIdName] = correlationId
                },
                PartitionKey = (message as IPartitioned)?.PartitionKey
            });
        }

        private byte[] SerializeMessage(object message)
        {
            string value = _messageSerializer.Serialize(message);
            return Encoding.UTF8.GetBytes(value);
        }

        /// <summary>
        /// Deserializes <see cref="Envelope"/> from <see cref="Message"/>.
        /// </summary>
        /// <param name="data"><see cref="Message"/> that contains serialized data.</param>
        /// <returns>A task representing the asynchronous operation. The task result contains an <see cref="Envelope"/> instance deserialized.</returns>
        public Task<Envelope> Deserialize(Message data)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            data.UserProperties.TryGetValue(MessageIdName, out object messageId);
            data.UserProperties.TryGetValue(CorrelationIdName, out object correlationId);
            object message = DeserializeMessage(data.Body);

            return Task.FromResult(new Envelope(
                ParseGuid(messageId) ?? Guid.NewGuid(),
                ParseGuid(correlationId),
                message));
        }

        private object DeserializeMessage(byte[] body)
        {
            string value = Encoding.UTF8.GetString(body);
            return _messageSerializer.Deserialize(value);
        }
    }
}
