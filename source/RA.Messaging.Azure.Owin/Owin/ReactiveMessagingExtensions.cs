namespace Owin
{
    using System;
    using System.Threading;
    using Microsoft.Owin.BuilderProperties;
    using Microsoft.ServiceBus.Messaging;
    using ReactiveArchitecture.Messaging;
    using ReactiveArchitecture.Messaging.Azure;

    public static class ReactiveMessagingExtensions
    {
        public static void UseEventMessageProcessor(
            this IAppBuilder app,
            EventProcessorHost eventProcessorHost,
            IMessageHandler messageHandler,
            IMessageSerializer messageSerializer)
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

            var properties = new AppProperties(app.Properties);
            CancellationToken cancellationToken = properties.OnAppDisposing;

            var processorFactory = new EventMessageProcessorFactory(
                messageHandler,
                messageSerializer,
                cancellationToken);

            Start(eventProcessorHost, processorFactory);
            cancellationToken.Register(() => Stop(eventProcessorHost));
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

        public static void UseServiceBusQueueMessageProcessor(
            this IAppBuilder app,
            string connectionString,
            string queueName,
            IMessageHandler messageHandler,
            IMessageSerializer messageSerializer)
        {
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }

            if (connectionString == null)
            {
                throw new ArgumentNullException(nameof(connectionString));
            }

            if (queueName == null)
            {
                throw new ArgumentNullException(nameof(queueName));
            }

            if (messageHandler == null)
            {
                throw new ArgumentNullException(nameof(messageHandler));
            }

            if (messageSerializer == null)
            {
                throw new ArgumentNullException(nameof(messageSerializer));
            }

            ServiceBusQueueMessageProcessor.Run(
                connectionString,
                queueName,
                messageHandler,
                messageSerializer,
                new AppProperties(app.Properties).OnAppDisposing);
        }
    }
}
