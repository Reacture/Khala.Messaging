namespace ReactiveArchitecture.Messaging.Azure
{
    using System;
    using System.Threading;
    using Microsoft.ServiceBus.Messaging;

    public class EventMessageProcessorFactory : IEventProcessorFactory
    {
        private readonly IMessageHandler _messageHandler;
        private readonly IMessageSerializer _messageSerializer;
        private readonly IMessageProcessingExceptionHandler<EventData> _exceptionHandler;
        private readonly CancellationToken _cancellationToken;

        public EventMessageProcessorFactory(
            IMessageHandler messageHandler,
            IMessageSerializer messageSerializer,
            IMessageProcessingExceptionHandler<EventData> exceptionHandler,
            CancellationToken cancellationToken)
        {
            if (messageHandler == null)
            {
                throw new ArgumentNullException(nameof(messageHandler));
            }

            if (messageSerializer == null)
            {
                throw new ArgumentNullException(nameof(messageSerializer));
            }

            if (exceptionHandler == null)
            {
                throw new ArgumentNullException(nameof(exceptionHandler));
            }

            _messageHandler = messageHandler;
            _messageSerializer = messageSerializer;
            _exceptionHandler = exceptionHandler;
            _cancellationToken = cancellationToken;
        }

        public IEventProcessor CreateEventProcessor(PartitionContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            return new EventMessageProcessor(
                _messageHandler,
                _messageSerializer,
                _exceptionHandler,
                _cancellationToken);
        }
    }
}
