namespace Owin
{
    using System;
    using System.Threading;
    using Khala.Messaging;
    using Khala.Messaging.Azure;
    using Microsoft.Owin.BuilderProperties;
    using Microsoft.ServiceBus.Messaging;

    public static class EventHubMessagingExtensions
    {
        private static readonly IMessageProcessingExceptionHandler<EventData> _defaultExceptionHandler = new CompositeMessageProcessingExceptionHandler<EventData>();

        public static void UseEventMessageProcessor(
            this IAppBuilder app,
            EventProcessorHost eventProcessorHost,
            EventDataSerializer serializer,
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

            var processorFactory = new EventMessageProcessorFactory(
                serializer,
                messageHandler,
                exceptionHandler,
                cancellationToken);

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
                _defaultExceptionHandler);
        }

        private static void Start(
            EventProcessorHost eventProcessorHost,
            EventMessageProcessorFactory processorFactory) =>
            eventProcessorHost.RegisterEventProcessorFactoryAsync(processorFactory).Wait();

        private static void Stop(EventProcessorHost eventProcessorHost) =>
            eventProcessorHost.UnregisterEventProcessorAsync().Wait();
    }
}
