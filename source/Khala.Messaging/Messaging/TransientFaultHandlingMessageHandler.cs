namespace Khala.Messaging
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Khala.TransientFaultHandling;

    public class TransientFaultHandlingMessageHandler : IMessageHandler
    {
        private RetryPolicy _retryPolicy;
        private IMessageHandler _messageHandler;

        public TransientFaultHandlingMessageHandler(RetryPolicy retryPolicy, IMessageHandler messageHandler)
        {
            _retryPolicy = retryPolicy ?? throw new ArgumentNullException(nameof(retryPolicy));
            _messageHandler = messageHandler ?? throw new ArgumentNullException(nameof(messageHandler));
        }

        public Task Handle(Envelope envelope, CancellationToken cancellationToken)
        {
            if (envelope == null)
            {
                throw new ArgumentNullException(nameof(envelope));
            }

            return _retryPolicy.Run(() => _messageHandler.Handle(envelope, cancellationToken));
        }
    }
}
