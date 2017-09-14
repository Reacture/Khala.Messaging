namespace Khala.Messaging
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Khala.TransientFaultHandling;

    public class TransientFaultHandlingMessageBus : IMessageBus
    {
        public TransientFaultHandlingMessageBus(RetryPolicy retryPolicy, IMessageBus messageBus)
        {
            RetryPolicy = retryPolicy ?? throw new ArgumentNullException(nameof(retryPolicy));
            MessageBus = messageBus ?? throw new ArgumentNullException(nameof(messageBus));
        }

        public RetryPolicy RetryPolicy { get; }

        public IMessageBus MessageBus { get; }

        public Task Send(Envelope envelope, CancellationToken cancellationToken)
        {
            if (envelope == null)
            {
                throw new ArgumentNullException(nameof(envelope));
            }

            return RetryPolicy.Run(MessageBus.Send, envelope, cancellationToken);
        }

        public Task SendBatch(IEnumerable<Envelope> envelopes, CancellationToken cancellationToken)
        {
            if (envelopes == null)
            {
                throw new ArgumentNullException(nameof(envelopes));
            }

            return RetryPolicy.Run(MessageBus.SendBatch, envelopes, cancellationToken);
        }
    }
}
