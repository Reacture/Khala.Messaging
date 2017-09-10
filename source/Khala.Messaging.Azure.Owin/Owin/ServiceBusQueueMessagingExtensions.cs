namespace Owin
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Khala.Messaging;
    using Khala.Messaging.Azure;
    using Microsoft.Owin.BuilderProperties;
    using Microsoft.ServiceBus.Messaging;

    public static class ServiceBusQueueMessagingExtensions
    {
        public static void UseServiceBusQueueMessageProcessor(
            this IAppBuilder app,
            string connectionString,
            string queueName,
            IMessageDataSerializer<BrokeredMessage> serializer,
            IMessageHandler messageHandler,
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

            var queueClient = QueueClient.CreateFromConnectionString(connectionString, queueName);
            CancellationToken cancellationToken = new AppProperties(app.Properties).OnAppDisposing;
            var processorCore = new MessageProcessorCore<BrokeredMessage>(messageHandler, serializer, exceptionHandler);
            var processor = new BrokeredMessageProcessor(processorCore, cancellationToken);
            queueClient.OnMessageAsync(processor.ProcessMessage);
            cancellationToken.Register(queueClient.Close);
        }

        public static void UseServiceBusQueueMessageProcessor(
            this IAppBuilder app,
            string connectionString,
            string queueName,
            IMessageDataSerializer<BrokeredMessage> serializer,
            IMessageHandler messageHandler)
        {
            app.UseServiceBusQueueMessageProcessor(
                connectionString,
                queueName,
                serializer,
                messageHandler,
                DefaultExceptionHandler.Instance);
        }

        private class DefaultExceptionHandler : IMessageProcessingExceptionHandler<BrokeredMessage>
        {
            public static readonly DefaultExceptionHandler Instance = new DefaultExceptionHandler();

            public Task Handle(MessageProcessingExceptionContext<BrokeredMessage> context)
                => Task.FromResult(true);
        }
    }
}
