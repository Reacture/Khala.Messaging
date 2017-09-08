namespace Khala.Messaging
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Khala.TransientFaultHandling;

    public class TransientFaultHandlingMessageHandler : IMessageHandler
    {
        public TransientFaultHandlingMessageHandler(RetryPolicy retryPolicy, IMessageHandler messageHandler)
        {
            RetryPolicy = retryPolicy ?? throw new ArgumentNullException(nameof(retryPolicy));
            MessageHandler = messageHandler ?? throw new ArgumentNullException(nameof(messageHandler));
        }

        public RetryPolicy RetryPolicy { get; }

        public IMessageHandler MessageHandler { get; }

        public Task Handle(Envelope envelope, CancellationToken cancellationToken)
        {
            if (envelope == null)
            {
                throw new ArgumentNullException(nameof(envelope));
            }

            return RetryPolicy.Run(() => MessageHandler.Handle(envelope, cancellationToken));
        }
    }
}
