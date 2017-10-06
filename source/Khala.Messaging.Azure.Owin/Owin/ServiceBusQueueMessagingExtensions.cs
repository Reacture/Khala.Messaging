namespace Owin
{
    using System;
    using System.Threading.Tasks;
    using Khala.Messaging;
    using Khala.Messaging.Azure;
    using Microsoft.Azure.ServiceBus;
    using Microsoft.Owin.BuilderProperties;

    public static class ServiceBusQueueMessagingExtensions
    {
        public static void UseServiceBusQueueMessageProcessor(
            this IAppBuilder app,
            string connectionString,
            string queueName,
            IMessageDataSerializer<Message> serializer,
            IMessageHandler messageHandler,
            IMessageProcessingExceptionHandler<Message> exceptionHandler)
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

            var queueClient = new QueueClient(connectionString, queueName);

            var appProperties = new AppProperties(app.Properties);

            var processor = new ServiceBusMessageProcessor(
                queueClient,
                new MessageProcessorCore<Message>(
                    messageHandler,
                    serializer,
                    exceptionHandler),
                cancellationToken: appProperties.OnAppDisposing);

            queueClient.RegisterMessageHandler(
                (message, cancellationToken) => processor.ProcessMessage(message),
                new MessageHandlerOptions(ExceptionReceivedHandler) { AutoComplete = false });

            appProperties.OnAppDisposing.Register(() => queueClient.CloseAsync().Wait());
        }

        private static Task ExceptionReceivedHandler(ExceptionReceivedEventArgs exceptionReceivedEventArgs) => Task.CompletedTask;

        public static void UseServiceBusQueueMessageProcessor(
            this IAppBuilder app,
            string connectionString,
            string queueName,
            IMessageDataSerializer<Message> serializer,
            IMessageHandler messageHandler)
        {
            app.UseServiceBusQueueMessageProcessor(
                connectionString,
                queueName,
                serializer,
                messageHandler,
                DefaultExceptionHandler.Instance);
        }

        private class DefaultExceptionHandler : IMessageProcessingExceptionHandler<Message>
        {
            public static readonly DefaultExceptionHandler Instance = new DefaultExceptionHandler();

            public Task Handle(MessageProcessingExceptionContext<Message> context) => Task.CompletedTask;
        }
    }
}
