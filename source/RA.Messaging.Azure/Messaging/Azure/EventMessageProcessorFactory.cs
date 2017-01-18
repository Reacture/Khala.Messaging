namespace ReactiveArchitecture.Messaging.Azure
{
    using System;
    using Microsoft.ServiceBus.Messaging;

    public class EventMessageProcessorFactory : IEventProcessorFactory
    {
        private readonly IMessageHandler _handler;
        private readonly IMessageSerializer _serializer;

        public EventMessageProcessorFactory(
            IMessageHandler handler,
            IMessageSerializer serializer)
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
        }

        public IEventProcessor CreateEventProcessor(PartitionContext context)
            => new EventMessageProcessor(_handler, _serializer);
    }
}
