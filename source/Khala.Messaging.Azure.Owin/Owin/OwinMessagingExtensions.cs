namespace Owin
{
    using System;
    using System.Threading;
    using Khala.Messaging;
    using Khala.Messaging.Azure;
    using Microsoft.Azure.EventHubs.Processor;
    using Microsoft.Owin.BuilderProperties;

    public static class OwinMessagingExtensions
    {
        public static void UseEventProcessor(
            this IAppBuilder appBuilder,
            EventProcessorHost eventProcessorHost,
            IMessageHandler messageHandler,
            IEventProcessingExceptionHandler exceptionHandler)
        {
            if (appBuilder == null)
            {
                throw new ArgumentNullException(nameof(appBuilder));
            }

            if (eventProcessorHost == null)
            {
                throw new ArgumentNullException(nameof(eventProcessorHost));
            }

            var appProperties = new AppProperties(appBuilder.Properties);
            CancellationToken cancellationToken = appProperties.OnAppDisposing;
            var processorFactory = new EventProcessorFactory(messageHandler, exceptionHandler, cancellationToken);
            eventProcessorHost.RegisterEventProcessorFactoryAsync(processorFactory).Wait();
            cancellationToken.Register(() => eventProcessorHost.UnregisterEventProcessorAsync().Wait());
        }
    }
}
