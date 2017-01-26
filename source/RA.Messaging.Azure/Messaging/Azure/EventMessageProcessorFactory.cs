namespace ReactiveArchitecture.Messaging.Azure
{
    using System;
    using System.Threading;
    using Microsoft.ServiceBus.Messaging;

    public class EventMessageProcessorFactory : IEventProcessorFactory
    {
        private readonly IMessageHandler _handler;
        private readonly IMessageSerializer _serializer;
        private readonly IMessageProcessingExceptionHandler<EventData> _exceptionHandler;
        private readonly CancellationToken _cancellationToken;

        public EventMessageProcessorFactory(
            IMessageHandler handler,
            IMessageSerializer serializer,
            IMessageProcessingExceptionHandler<EventData> exceptionHandler,
            CancellationToken cancellationToken)
        {
            if (handler == null)
            {
                throw new ArgumentNullException(nameof(handler));
            }

            if (serializer == null)
            {
                throw new ArgumentNullException(nameof(serializer));
            }

            if (exceptionHandler == null)
            {
                throw new ArgumentNullException(nameof(exceptionHandler));
            }

            _handler = handler;
            _serializer = serializer;
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
                _handler,
                _serializer,
                _exceptionHandler,
                _cancellationToken);
        }
    }
}
