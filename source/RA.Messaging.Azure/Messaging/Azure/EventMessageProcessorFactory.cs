namespace ReactiveArchitecture.Messaging.Azure
{
    using System;
    using System.Threading;
    using Microsoft.ServiceBus.Messaging;

    public class EventMessageProcessorFactory : IEventProcessorFactory
    {
        private readonly IMessageHandler _handler;
        private readonly IMessageSerializer _serializer;
        private readonly CancellationToken _cancellationToken;

        public EventMessageProcessorFactory(
            IMessageHandler handler,
            IMessageSerializer serializer,
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

            _handler = handler;
            _serializer = serializer;
            _cancellationToken = cancellationToken;
        }

        public IEventProcessor CreateEventProcessor(PartitionContext context)
        {
            return new EventMessageProcessor(
                _handler,
                _serializer,
                _cancellationToken);
        }
    }
}
