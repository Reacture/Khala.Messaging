namespace Khala.Messaging.Azure
{
    using System;
    using System.IO;
    using System.Text;
    using System.Threading.Tasks;
    using Microsoft.ServiceBus.Messaging;

    /// <summary>
    /// Saves <see cref="Envelope"/> data to <see cref="EventData"/> and restore.
    /// </summary>
    public class EventDataSerializer
    {
        private readonly IMessageSerializer _messageSerializer;

        /// <summary>
        /// Initializes a new instance of the <see cref="EventDataSerializer"/> class.
        /// </summary>
        public EventDataSerializer()
            : this(new JsonMessageSerializer())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EventDataSerializer"/> class with an <see cref="IMessageSerializer"/>.
        /// </summary>
        /// <param name="messageSerializer"><see cref="IMessageSerializer"/> to serialize enveloped messages.</param>
        public EventDataSerializer(IMessageSerializer messageSerializer)
        {
            if (messageSerializer == null)
            {
                throw new ArgumentNullException(nameof(messageSerializer));
            }

            _messageSerializer = messageSerializer;
        }

        /// <summary>
        /// Serializes <see cref="Envelope"/> instance to <see cref="EventData"/>.
        /// </summary>
        /// <param name="envelope"><see cref="Envelope"/> to serialize.</param>
        /// <returns>A task representing the asynchronous operation. The task result contains an <see cref="EventData"/> that contains serialized data.</returns>
        public Task<EventData> Serialize(Envelope envelope)
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

            return Task.FromResult(new EventData(body)
            {
                Properties =
                {
                    ["Khala.Envelope.MessageId"] = messageId,
                    ["Khala.Envelope.CorrelationId"] = correlationId
                },
                PartitionKey = (message as IPartitioned)?.PartitionKey
            });
        }

        /// <summary>
        /// Deserializes <see cref="Envelope"/> from <see cref="EventData"/>.
        /// </summary>
        /// <param name="eventData"><see cref="EventData"/> that contains serialized data.</param>
        /// <returns>A task representing the asynchronous operation. The task result contains an <see cref="Envelope"/> instance deserialized.</returns>
        public Task<Envelope> Deserialize(EventData eventData)
        {
            if (eventData == null)
            {
                throw new ArgumentNullException(nameof(eventData));
            }

            return DeserialzeEnvelope(eventData);
        }

        private static Guid? ParseGuid(object property)
        {
            Guid value;
            return Guid.TryParse(property?.ToString(), out value)
                ? value
                : default(Guid?);
        }

        private async Task<Envelope> DeserialzeEnvelope(EventData eventData)
        {
            object messageId;
            eventData.Properties.TryGetValue(
                "Khala.Envelope.MessageId", out messageId);

            object correlationId;
            eventData.Properties.TryGetValue(
                "Khala.Envelope.CorrelationId", out correlationId);

            using (Stream stream = eventData.GetBodyStream())
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
