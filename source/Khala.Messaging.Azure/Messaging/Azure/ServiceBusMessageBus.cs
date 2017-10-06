namespace Khala.Messaging.Azure
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.ServiceBus;
    using Microsoft.Azure.ServiceBus.Core;

    public sealed class ServiceBusMessageBus : IMessageBus
    {
        private readonly ISenderClient _senderClient;
        private readonly ServiceBusMessageSerializer _serializer;

        public ServiceBusMessageBus(
            ISenderClient senderClient,
            ServiceBusMessageSerializer serializer)
        {
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            _senderClient = senderClient ?? throw new ArgumentNullException(nameof(senderClient));
        }

        public ServiceBusMessageBus(
            ISenderClient senderClient,
            IMessageSerializer messageSerializer)
            : this(senderClient, new ServiceBusMessageSerializer(messageSerializer))
        {
        }

        public ServiceBusMessageBus(ISenderClient senderClient)
            : this(senderClient, new ServiceBusMessageSerializer())
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

            return RunSend(envelope);
        }

        private async Task RunSend(Envelope envelope)
        {
            Message message = await _serializer.Serialize(envelope).ConfigureAwait(false);
            await _senderClient.SendAsync(message).ConfigureAwait(false);
        }

        public Task Send(
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

            return RunSend(envelopeList);
        }

        private async Task RunSend(IEnumerable<Envelope> envelopes)
        {
            var messages = new List<Message>();

            foreach (Envelope envelope in envelopes)
            {
                Message brokeredMessage = await _serializer.Serialize(envelope).ConfigureAwait(false);
                messages.Add(brokeredMessage);
            }

            await _senderClient.SendAsync(messages).ConfigureAwait(false);
        }
    }
}
