namespace Owin
{
    using System;
    using System.Threading.Tasks;
    using Khala.Messaging;
    using Khala.Messaging.Azure;
    using Microsoft.Azure.EventHubs;
    using Microsoft.Azure.EventHubs.Processor;
    using Microsoft.Owin.BuilderProperties;

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

            var appProperties = new AppProperties(app.Properties);

            var processorFactory = new EventMessageProcessorFactory(
                new MessageProcessorCore<EventData>(
                    messageHandler,
                    serializer,
                    exceptionHandler),
                appProperties.OnAppDisposing);

            Start(eventProcessorHost, processorFactory);

            appProperties.OnAppDisposing.Register(() => Stop(eventProcessorHost));
        }

        public static void UseEventMessageProcessor(
            this IAppBuilder app,
            EventProcessorHost eventProcessorHost,
            IMessageDataSerializer<EventData> serializer,
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

        private static void Stop(
            EventProcessorHost eventProcessorHost) =>
            eventProcessorHost.UnregisterEventProcessorAsync().Wait();

        private class DefaultExceptionHandler : IMessageProcessingExceptionHandler<EventData>
        {
            public static readonly DefaultExceptionHandler Instance = new DefaultExceptionHandler();

            public Task Handle(MessageProcessingExceptionContext<EventData> context) => Task.CompletedTask;
        }
    }
}
