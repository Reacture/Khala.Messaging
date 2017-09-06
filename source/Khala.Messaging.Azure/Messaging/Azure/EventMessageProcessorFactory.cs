namespace Khala.Messaging.Azure
{
    using System;
    using System.Threading;
    using Microsoft.ServiceBus.Messaging;

    public sealed class EventMessageProcessorFactory : IEventProcessorFactory
    {
        private readonly MessageProcessorCore<EventData> _processorCore;
        private readonly CancellationToken _cancellationToken;

        public EventMessageProcessorFactory(
            MessageProcessorCore<EventData> processorCore,
            CancellationToken cancellationToken)
        {
            _processorCore = processorCore ?? throw new ArgumentNullException(nameof(processorCore));
            _cancellationToken = cancellationToken;
        }

        public IEventProcessor CreateEventProcessor(PartitionContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            return new EventMessageProcessor(_processorCore, _cancellationToken);
        }
    }
}
