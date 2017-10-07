namespace Owin
{
    using System;
    using System.Threading;
    using Khala.Messaging;
    using Khala.Messaging.Azure;
    using Microsoft.Azure.EventHubs;
    using Microsoft.Azure.EventHubs.Processor;
    using Microsoft.Owin.BuilderProperties;

    public static class OwinMessagingExtensions
    {
        public static void UseEventProcessor(
            this IAppBuilder app,
            EventProcessorHost eventProcessorHost,
            MessageProcessor<EventData> messageProcessor)
        {
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }

            if (eventProcessorHost == null)
            {
                throw new ArgumentNullException(nameof(eventProcessorHost));
            }

            if (messageProcessor == null)
            {
                throw new ArgumentNullException(nameof(messageProcessor));
            }

            CancellationToken cancellationToken = new AppProperties(app.Properties).OnAppDisposing;
            Start(eventProcessorHost, new EventProcessorFactory(messageProcessor, cancellationToken));
            cancellationToken.Register(() => Stop(eventProcessorHost));
        }

        private static void Start(
            EventProcessorHost eventProcessorHost,
            EventProcessorFactory eventProcessorFactory) =>
            eventProcessorHost.RegisterEventProcessorFactoryAsync(eventProcessorFactory).Wait();

        private static void Stop(
            EventProcessorHost eventProcessorHost) =>
            eventProcessorHost.UnregisterEventProcessorAsync().Wait();
    }
}
