namespace Owin
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Khala.Messaging;
    using Khala.Messaging.Azure;
    using Microsoft.Owin.BuilderProperties;
    using Microsoft.ServiceBus.Messaging;

    public static class EventHubMessagingExtensions
    {
        public static void UseEventMessageProcessor(
            this IAppBuilder app,
            EventProcessorHost eventProcessorHost,
            IMessageDataSerializer<EventData> serializer,
            IMessageHandler messageHandler,
            IMessageProcessingExceptionHandler<EventData> exceptionHandler)
        {
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }

            if (eventProcessorHost == null)
            {
                throw new ArgumentNullException(nameof(eventProcessorHost));
            }

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

            CancellationToken cancellationToken = new AppProperties(app.Properties).OnAppDisposing;
            var processorCore = new MessageProcessorCore<EventData>(messageHandler, serializer, exceptionHandler);
            var processorFactory = new EventMessageProcessorFactory(processorCore, cancellationToken);
            Start(eventProcessorHost, processorFactory);
            cancellationToken.Register(() => Stop(eventProcessorHost));
        }

        public static void UseEventMessageProcessor(
            this IAppBuilder app,
            EventProcessorHost eventProcessorHost,
            EventDataSerializer serializer,
            IMessageHandler messageHandler)
        {
            app.UseEventMessageProcessor(
                eventProcessorHost,
                serializer,
                messageHandler,
                DefaultExceptionHandler.Instance);
        }

        private static void Start(
            EventProcessorHost eventProcessorHost,
            EventMessageProcessorFactory processorFactory) =>
            eventProcessorHost.RegisterEventProcessorFactoryAsync(processorFactory).Wait();

        private static void Stop(EventProcessorHost eventProcessorHost) =>
            eventProcessorHost.UnregisterEventProcessorAsync().Wait();

        private class DefaultExceptionHandler : IMessageProcessingExceptionHandler<EventData>
        {
            public static readonly DefaultExceptionHandler Instance = new DefaultExceptionHandler();

            public Task Handle(MessageProcessingExceptionContext<EventData> context)
                => Task.FromResult(true);
        }
    }
}
