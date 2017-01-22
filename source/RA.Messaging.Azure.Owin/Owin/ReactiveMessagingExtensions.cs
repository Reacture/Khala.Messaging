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
            IMessageSerializer messageSerializer,
            IEventMessageExceptionHandler exceptionHandler)
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
            IMessageSerializer messageSerializer,
            IBrokeredMessageExceptionHandler exceptionHandler)
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

            if (exceptionHandler == null)
            {
                throw new ArgumentNullException(nameof(exceptionHandler));
            }

            ServiceBusQueueMessageProcessor.Process(
                connectionString,
                queueName,
                messageHandler,
                messageSerializer,
                exceptionHandler,
                new AppProperties(app.Properties).OnAppDisposing);
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

            ServiceBusQueueMessageProcessor.Process(
                connectionString,
                queueName,
                messageHandler,
                messageSerializer,
                new AppProperties(app.Properties).OnAppDisposing);
        }
    }
}
