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
        private const string MessageIdName = "Khala.Messaging.Envelope.MessageId";
        private const string OperationIdName = "Khala.Messaging.Envelope.OperationId";
        private const string CorrelationIdName = "Khala.Messaging.Envelope.CorrelationId";
        private const string ContributorName = "Khala.Messaging.Envelope.Contributor";

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
                    [MessageIdName] = envelope.MessageId.ToString("n"),
                    [OperationIdName] = envelope.OperationId?.ToString("n"),
                    [CorrelationIdName] = envelope.CorrelationId?.ToString("n"),
                    [ContributorName] = envelope.Contributor
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
                GetOperationId(data.Properties),
                GetCorrelationId(data.Properties),
                GetContributor(data.Properties),
                DeserializeMessage(data.Body));
        }

        private object DeserializeMessage(ArraySegment<byte> body)
        {
            string value = Encoding.UTF8.GetString(body.Array, body.Offset, body.Count);
            return _messageSerializer.Deserialize(value);
        }

        private Guid GetMessageId(IDictionary<string, object> properties)
        {
            properties.TryGetValue(MessageIdName, out object value);
            return Guid.TryParse(value?.ToString(), out Guid messageId) ? messageId : Guid.NewGuid();
        }

        private Guid? GetOperationId(IDictionary<string, object> properties)
        {
            properties.TryGetValue(OperationIdName, out object value);
            return Guid.TryParse(value?.ToString(), out Guid operationId) ? operationId : default(Guid?);
        }

        private Guid? GetCorrelationId(IDictionary<string, object> properties)
        {
            properties.TryGetValue(CorrelationIdName, out object value);
            return Guid.TryParse(value?.ToString(), out Guid correlationId) ? correlationId : default(Guid?);
        }

        private string GetContributor(IDictionary<string, object> properties)
        {
            properties.TryGetValue(ContributorName, out object value);
            return value?.ToString();
        }
    }
}
