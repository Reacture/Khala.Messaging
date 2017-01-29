namespace Owin
{
    using System;
    using System.Threading;
    using Arcane.Messaging;
    using Arcane.Messaging.Azure;
    using Microsoft.Owin.BuilderProperties;
    using Microsoft.ServiceBus.Messaging;

    public static class ServiceBusQueueMessagingExtensions
    {
        private static readonly IMessageProcessingExceptionHandler<BrokeredMessage> _defaultExceptionHandler = new CompositeMessageProcessingExceptionHandler<BrokeredMessage>();

        public static void UseServiceBusQueueMessageProcessor(
            this IAppBuilder app,
            string connectionString,
            string queueName,
            IMessageHandler messageHandler,
            IMessageSerializer messageSerializer,
            IMessageProcessingExceptionHandler<BrokeredMessage> exceptionHandler)
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

            var queueClient = QueueClient.CreateFromConnectionString(connectionString, queueName);

            CancellationToken cancellationToken = new AppProperties(app.Properties).OnAppDisposing;

            var processor = new BrokeredMessageProcessor(
                messageHandler,
                messageSerializer,
                exceptionHandler,
                cancellationToken);

            queueClient.OnMessageAsync(processor.ProcessMessage);

            cancellationToken.Register(queueClient.Close);
        }

        public static void UseServiceBusQueueMessageProcessor(
            this IAppBuilder app,
            string connectionString,
            string queueName,
            IMessageHandler messageHandler,
            IMessageSerializer messageSerializer)
        {
            app.UseServiceBusQueueMessageProcessor(
                connectionString,
                queueName,
                messageHandler,
                messageSerializer,
                _defaultExceptionHandler);
        }
    }
}
