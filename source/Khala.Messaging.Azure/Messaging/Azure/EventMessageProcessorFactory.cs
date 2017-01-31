namespace Khala.Messaging.Azure
{
    using System;
    using System.Threading;
    using Microsoft.ServiceBus.Messaging;

    public class EventMessageProcessorFactory : IEventProcessorFactory
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
            if (serializer == null)
            {
                throw new ArgumentNullException(nameof(serializer));
            }

            if (messageHandler == null)
            {
                throw new ArgumentNullException(nameof(messageHandler));
            }

            if (exceptionHandler == null)
            {
                throw new ArgumentNullException(nameof(exceptionHandler));
            }

            _serializer = serializer;
            _messageHandler = messageHandler;
            _exceptionHandler = exceptionHandler;
            _cancellationToken = cancellationToken;
        }

        public IEventProcessor CreateEventProcessor(PartitionContext context)
            => new EventMessageProcessor(_serializer, _messageHandler, _exceptionHandler, _cancellationToken);
    }
}
