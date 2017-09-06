namespace Khala.Messaging.Azure
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.ServiceBus.Messaging;

    public sealed class BrokeredMessageProcessor
    {
        private readonly MessageProcessorCore<BrokeredMessage> _processorCore;
        private readonly CancellationToken _cancellationToken;

        public BrokeredMessageProcessor(
            MessageProcessorCore<BrokeredMessage> processorCore,
            CancellationToken cancellationToken)
        {
            _processorCore = processorCore;
            _cancellationToken = cancellationToken;
        }

        public Task ProcessMessage(BrokeredMessage brokeredMessage)
        {
            if (brokeredMessage == null)
            {
                throw new ArgumentNullException(nameof(brokeredMessage));
            }

            return Process(brokeredMessage);
        }

        private static Task CompleteMessage(BrokeredMessage brokeredMessage) => brokeredMessage.CompleteAsync();

        private Task Process(BrokeredMessage brokeredMessage)
            => _processorCore.Process(brokeredMessage, CompleteMessage, _cancellationToken);
    }
}
