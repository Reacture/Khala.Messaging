namespace Khala.Messaging.Azure
{
    using System;
    using System.Text;
    using System.Threading.Tasks;
    using Microsoft.Azure.EventHubs;

    /// <summary>
    /// Serializes and deserializes <see cref="Envelope"/> objects into and from <see cref="EventData"/>.
    /// </summary>
    public sealed class EventDataSerializer : IMessageDataSerializer<EventData>
    {
        private const string MessageIdName = "Khala.Messaging.Envelope.MessageId";
        private const string CorrelationIdName = "Khala.Messaging.Envelope.CorrelationId";

        private readonly IMessageSerializer _messageSerializer;

        /// <summary>
        /// Initializes a new instance of the <see cref="EventDataSerializer"/> class with an <see cref="IMessageSerializer"/>.
        /// </summary>
        /// <param name="messageSerializer"><see cref="IMessageSerializer"/> to serialize enveloped messages.</param>
        public EventDataSerializer(IMessageSerializer messageSerializer)
        {
            _messageSerializer = messageSerializer ?? throw new ArgumentNullException(nameof(messageSerializer));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EventDataSerializer"/> class.
        /// </summary>
        public EventDataSerializer()
            : this(new JsonMessageSerializer())
        {
        }

        private static Guid? ParseGuid(object property)
            => Guid.TryParse(property?.ToString(), out Guid value)
            ? value
            : default(Guid?);

        /// <summary>
        /// Serializes <see cref="Envelope"/> instance into <see cref="EventData"/>.
        /// </summary>
        /// <param name="envelope"><see cref="Envelope"/> to serialize.</param>
        /// <returns>A task representing the asynchronous operation. The task result contains an <see cref="EventData"/> that contains serialized data.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "Serialize() method returns EventData asynchronously.")]
        public Task<EventData> Serialize(Envelope envelope)
        {
            if (envelope == null)
            {
                throw new ArgumentNullException(nameof(envelope));
            }

            return Task.FromResult(new EventData(SerializeMessage(envelope.Message))
            {
                Properties =
                {
                    [MessageIdName] = envelope.MessageId.ToString("n"),
                    [CorrelationIdName] = envelope.CorrelationId?.ToString("n")
                }
            });
        }

        private byte[] SerializeMessage(object message)
        {
            string value = _messageSerializer.Serialize(message);
            return Encoding.UTF8.GetBytes(value);
        }

        /// <summary>
        /// Deserializes <see cref="Envelope"/> from <see cref="EventData"/>.
        /// </summary>
        /// <param name="data"><see cref="EventData"/> that contains serialized data.</param>
        /// <returns>A task representing the asynchronous operation. The task result contains an <see cref="Envelope"/> instance deserialized.</returns>
        public Task<Envelope> Deserialize(EventData data)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            data.Properties.TryGetValue(MessageIdName, out object messageId);
            data.Properties.TryGetValue(CorrelationIdName, out object correlationId);
            object message = DeserializeMessage(data.Body);

            return Task.FromResult(new Envelope(
                ParseGuid(messageId) ?? Guid.NewGuid(),
                ParseGuid(correlationId),
                message));
        }

        private object DeserializeMessage(ArraySegment<byte> body)
        {
            string value = Encoding.UTF8.GetString(body.Array, body.Offset, body.Count);
            return _messageSerializer.Deserialize(value);
        }
    }
}
