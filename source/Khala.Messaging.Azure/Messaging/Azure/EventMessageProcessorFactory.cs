namespace Khala.Messaging.Azure
{
    using System;
    using System.Threading;
    using Microsoft.ServiceBus.Messaging;

    public sealed class EventMessageProcessorFactory : IEventProcessorFactory
    {
        private readonly EventDataSerializer _serializer;
        private readonly IMessageHandler _messageHandler;
        private readonly IMessageProcessingExceptionHandler<EventData> _exceptionHandler;
        private readonly CancellationToken _cancellationToken;

        public EventMessageProcessorFactory(
            EventDataSerializer serializer,
            IMessageHandler messageHandler,
            IMessageProcessingExceptionHandler<EventData> exceptionHandler,
            CancellationToken cancellationToken)
        {
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
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

            return new EventMessageProcessor(_serializer, _messageHandler, _exceptionHandler, _cancellationToken);
        }
    }
}
