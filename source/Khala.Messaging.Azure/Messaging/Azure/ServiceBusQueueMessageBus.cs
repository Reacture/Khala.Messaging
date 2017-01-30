namespace Khala.Messaging.Azure
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.ServiceBus.Messaging;

    public class ServiceBusQueueMessageBus : IMessageBus
    {
        private readonly QueueClient _queueClient;
        private readonly IMessageSerializer _serializer;

        public ServiceBusQueueMessageBus(
            QueueClient queueClient, IMessageSerializer serializer)
        {
            if (queueClient == null)
            {
                throw new ArgumentNullException(nameof(queueClient));
            }

            if (serializer == null)
            {
                throw new ArgumentNullException(nameof(serializer));
            }

            _queueClient = queueClient;
            _serializer = serializer;
        }

        public Task Send(
            Envelope envelope,
            CancellationToken cancellationToken)
        {
            if (envelope == null)
            {
                throw new ArgumentNullException(nameof(envelope));
            }

            BrokeredMessage brokeredMessage = GetBrokeredMessage(envelope);
            return _queueClient.SendAsync(brokeredMessage);
        }

        public Task SendBatch(
            IEnumerable<Envelope> envelopes,
            CancellationToken cancellationToken)
        {
            if (envelopes == null)
            {
                throw new ArgumentNullException(nameof(envelopes));
            }

            var brokeredMessages = new List<BrokeredMessage>();

            foreach (Envelope envelope in envelopes)
            {
                if (envelope == null)
                {
                    throw new ArgumentException(
                        $"{nameof(envelopes)} cannot contain null.",
                        nameof(envelopes));
                }

                brokeredMessages.Add(GetBrokeredMessage(envelope));
            }

            return _queueClient.SendBatchAsync(brokeredMessages);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "GetBrokeredMessage() returns an instance of BrokeredMessage.")]
        private BrokeredMessage GetBrokeredMessage(Envelope envelope)
        {
            string data = _serializer.Serialize(envelope);
            byte[] bytes = Encoding.UTF8.GetBytes(data);
            var brokeredMessage = new BrokeredMessage(
                messageBodyStream: new MemoryStream(bytes),
                ownsStream: true)
            {
                MessageId = envelope.MessageId.ToString("n"),
                CorrelationId = envelope.CorrelationId?.ToString("n")
            };

            var partitioned = envelope.Message as IPartitioned;
            if (partitioned != null)
            {
                brokeredMessage.PartitionKey = partitioned.PartitionKey;
            }

            return brokeredMessage;
        }
    }
}
