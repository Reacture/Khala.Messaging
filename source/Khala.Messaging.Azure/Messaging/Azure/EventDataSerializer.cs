namespace Khala.Messaging.Azure
{
    using System;
    using System.IO;
    using System.Text;
    using System.Threading.Tasks;
    using Microsoft.ServiceBus.Messaging;

    /// <summary>
    /// Serializes and deserializes <see cref="Envelope"/> objects into and from <see cref="EventData"/>.
    /// </summary>
    public sealed class EventDataSerializer : IMessageDataSerializer<EventData>
    {
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

            object message = envelope.Message;

            string value = _messageSerializer.Serialize(message);
            byte[] body = Encoding.UTF8.GetBytes(value);

            var messageId = envelope.MessageId.ToString("n");
            var correlationId = envelope.CorrelationId?.ToString("n");

            return Task.FromResult(new EventData(body)
            {
                Properties =
                {
                    ["Khala.Messaging.Envelope.MessageId"] = messageId,
                    ["Khala.Messaging.Envelope.CorrelationId"] = correlationId
                },
                PartitionKey = (message as IPartitioned)?.PartitionKey
            });
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

            async Task<Envelope> Run()
            {
                data.Properties.TryGetValue(
                    "Khala.Messaging.Envelope.MessageId", out object messageId);

                data.Properties.TryGetValue(
                    "Khala.Messaging.Envelope.CorrelationId", out object correlationId);

                using (Stream stream = data.GetBodyStream())
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

            return Run();
        }

        private static Guid? ParseGuid(object property)
        {
            return Guid.TryParse(property?.ToString(), out Guid value) ? value : default(Guid?);
        }
    }
}
