namespace Khala.Messaging.Azure
{
    using System;
    using System.Threading;
    using Microsoft.Azure.EventHubs.Processor;

    public sealed class EventProcessorFactory : IEventProcessorFactory
    {
        private readonly IMessageHandler _messageHandler;
        private readonly IEventProcessingExceptionHandler _exceptionHandler;
        private readonly CancellationToken _cancellationToken;

        public EventProcessorFactory(
            IMessageHandler messageHandler,
            IEventProcessingExceptionHandler exceptionHandler,
            CancellationToken cancellationToken)
        {
            _messageHandler = messageHandler ?? throw new ArgumentNullException(nameof(messageHandler));
            _exceptionHandler = exceptionHandler ?? throw new ArgumentNullException(nameof(exceptionHandler));
            _cancellationToken = cancellationToken;
        }

        public IEventProcessor CreateEventProcessor(PartitionContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            return new EventProcessor(_messageHandler, _exceptionHandler, _cancellationToken);
        }
    }
}
