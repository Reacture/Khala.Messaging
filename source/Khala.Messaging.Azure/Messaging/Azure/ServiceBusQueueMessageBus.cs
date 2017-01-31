namespace Khala.Messaging.Azure
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.ServiceBus.Messaging;

    public class ServiceBusQueueMessageBus : IMessageBus
    {
        private readonly BrokeredMessageSerializer _serializer;
        private readonly QueueClient _queueClient;

        public ServiceBusQueueMessageBus(
            BrokeredMessageSerializer serializer,
            QueueClient queueClient)
        {
            if (serializer == null)
            {
                throw new ArgumentNullException(nameof(serializer));
            }

            if (queueClient == null)
            {
                throw new ArgumentNullException(nameof(queueClient));
            }

            _serializer = serializer;
            _queueClient = queueClient;
        }

        public ServiceBusQueueMessageBus(
            IMessageSerializer messageSerializer,
            QueueClient queueClient)
            : this(new BrokeredMessageSerializer(messageSerializer), queueClient)
        {
        }

        public ServiceBusQueueMessageBus(QueueClient queueClient)
            : this(new BrokeredMessageSerializer(), queueClient)
        {
        }

        public Task Send(
            Envelope envelope,
            CancellationToken cancellationToken)
        {
            if (envelope == null)
            {
                throw new ArgumentNullException(nameof(envelope));
            }

            return SendMessage(envelope);
        }

        private async Task SendMessage(Envelope envelope)
        {
            BrokeredMessage brokeredMessage = await _serializer.Serialize(envelope).ConfigureAwait(false);
            await _queueClient.SendAsync(brokeredMessage).ConfigureAwait(false);
        }

        public Task SendBatch(
            IEnumerable<Envelope> envelopes,
            CancellationToken cancellationToken)
        {
            if (envelopes == null)
            {
                throw new ArgumentNullException(nameof(envelopes));
            }

            var envelopeList = new List<Envelope>();

            foreach (Envelope envelope in envelopes)
            {
                if (envelope == null)
                {
                    throw new ArgumentException(
                        $"{nameof(envelopes)} cannot contain null.",
                        nameof(envelopes));
                }

                envelopeList.Add(envelope);
            }

            return SendMessages(envelopeList);
        }

        private async Task SendMessages(IEnumerable<Envelope> envelopes)
        {
            var brokeredMessages = new List<BrokeredMessage>();

            foreach (Envelope envelope in envelopes)
            {
                BrokeredMessage brokeredMessage = await _serializer.Serialize(envelope).ConfigureAwait(false);
                brokeredMessages.Add(brokeredMessage);
            }

            await _queueClient.SendBatchAsync(brokeredMessages).ConfigureAwait(false);
        }
    }
}
