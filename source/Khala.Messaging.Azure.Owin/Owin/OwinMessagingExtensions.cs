namespace Owin
{
    using System;
    using System.Threading;
    using Khala.Messaging;
    using Khala.Messaging.Azure;
    using Microsoft.Azure.EventHubs.Processor;
    using Microsoft.Owin.BuilderProperties;

    /// <summary>
    /// Provides extension methods to support OWIN applications.
    /// </summary>
    public static class OwinMessagingExtensions
    {
        /// <summary>
        /// Starts an event processor that routes messages to an <see cref="IMessageHandler"/>.
        /// </summary>
        /// <param name="appBuilder">An <see cref="IAppBuilder"/> to initialize an OWIN applcation.</param>
        /// <param name="eventProcessorHost">An <see cref="EventProcessorHost"/> for processing event data.</param>
        /// <param name="messageHandler">A message handler object.</param>
        /// <param name="exceptionHandler">An exception handler object.</param>
        [Obsolete("This project is deprecated.")]
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

            var processorFactory = new EventProcessorFactory(messageHandler, exceptionHandler, CancellationToken.None);
            eventProcessorHost.RegisterEventProcessorFactoryAsync(processorFactory).Wait();

            CancellationToken appDisposing = new AppProperties(appBuilder.Properties).OnAppDisposing;
            appDisposing.Register(() => eventProcessorHost.UnregisterEventProcessorAsync().Wait());
        }
    }
}
