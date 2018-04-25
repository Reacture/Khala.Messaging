namespace Khala.Messaging.Azure
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using Microsoft.Azure.EventHubs;

    /// <summary>
    /// Serializes and deserializes <see cref="Envelope"/> objects into and from <see cref="EventData"/>.
    /// </summary>
    public sealed class EventDataSerializer
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
        public EventData Serialize(Envelope envelope)
        {
            if (envelope == null)
            {
                throw new ArgumentNullException(nameof(envelope));
            }

            return new EventData(SerializeMessage(envelope.Message))
            {
                Properties =
                {
                    [nameof(Envelope.MessageId)] = envelope.MessageId,
                    [nameof(Envelope.OperationId)] = envelope.OperationId,
                    [nameof(Envelope.CorrelationId)] = envelope.CorrelationId,
                    [nameof(Envelope.Contributor)] = envelope.Contributor
                }
            };
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
        public Envelope Deserialize(EventData data)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            return new Envelope(
                GetMessageId(data.Properties),
                DeserializeMessage(data.Body),
                GetOperationId(data.Properties),
                GetCorrelationId(data.Properties),
                GetContributor(data.Properties));
        }

        private object DeserializeMessage(ArraySegment<byte> body)
        {
            string value = Encoding.UTF8.GetString(body.Array, body.Offset, body.Count);
            return _messageSerializer.Deserialize(value);
        }

        private static Guid GetMessageId(IDictionary<string, object> properties)
        {
            properties.TryGetValue(nameof(Envelope.MessageId), out object value);
            switch (value)
            {
                case Guid messageId:
                    return messageId;

                default:
                    return Guid.NewGuid();
            }
        }

        private static Guid? GetOperationId(IDictionary<string, object> properties)
        {
            properties.TryGetValue(nameof(Envelope.OperationId), out object value);
            switch (value)
            {
                case Guid operationId:
                    return operationId;

                default:
                    return null;
            }
        }

        private static Guid? GetCorrelationId(IDictionary<string, object> properties)
        {
            properties.TryGetValue(nameof(Envelope.CorrelationId), out object value);
            switch (value)
            {
                case Guid correlationId:
                    return correlationId;

                default:
                    return null;
            }
        }

        private static string GetContributor(IDictionary<string, object> properties)
        {
            properties.TryGetValue(nameof(Envelope.Contributor), out object value);
            switch (value)
            {
                case string contributor:
                    return contributor;

                default:
                    return null;
            }
        }
    }
}
