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
            IMessageHandler messageHandler,
            IMessageSerializer messageSerializer,
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

            var properties = new AppProperties(app.Properties);
            CancellationToken cancellationToken = properties.OnAppDisposing;

            var processorFactory = new EventMessageProcessorFactory(
                messageHandler,
                messageSerializer,
                exceptionHandler,
                cancellationToken);

            Start(eventProcessorHost, processorFactory);
            cancellationToken.Register(() => Stop(eventProcessorHost));
        }

        public static void UseEventMessageProcessor(
            this IAppBuilder app,
            EventProcessorHost eventProcessorHost,
            IMessageHandler messageHandler,
            IMessageSerializer messageSerializer)
        {
            app.UseEventMessageProcessor(
                eventProcessorHost,
                messageHandler,
                messageSerializer,
                _defaultExceptionHandler);
        }

        private static void Start(
            EventProcessorHost eventProcessorHost,
            EventMessageProcessorFactory processorFactory)
        {
            eventProcessorHost
                .RegisterEventProcessorFactoryAsync(processorFactory)
                .Wait();
        }

        private static void Stop(EventProcessorHost eventProcessorHost)
        {
            eventProcessorHost
                .UnregisterEventProcessorAsync()
                .Wait();
        }
    }
}
