namespace ReactiveArchitecture.Messaging.Azure
{
    using System;
    using System.Threading;
    using Microsoft.ServiceBus.Messaging;

    public class EventMessageProcessorFactory : IEventProcessorFactory
    {
        private static readonly IEventMessageExceptionHandler _defaultExceptionHandler = new CompositeEventMessageExceptionHandler();

        private readonly IMessageHandler _handler;
        private readonly IMessageSerializer _serializer;
        private readonly IEventMessageExceptionHandler _exceptionHandler;
        private readonly CancellationToken _cancellationToken;

        public EventMessageProcessorFactory(
            IMessageHandler handler,
            IMessageSerializer serializer,
            IEventMessageExceptionHandler exceptionHandler,
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

        public EventMessageProcessorFactory(
            IMessageHandler handler,
            IMessageSerializer serializer,
            CancellationToken cancellationToken)
            : this(
                  handler,
                  serializer,
                  _defaultExceptionHandler,
                  cancellationToken)
        {
        }

        public IEventProcessor CreateEventProcessor(PartitionContext context)
        {
            return new EventMessageProcessor(
                _handler,
                _serializer,
                _exceptionHandler,
                _cancellationToken);
        }
    }
}
