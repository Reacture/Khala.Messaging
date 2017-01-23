namespace Owin
{
    using System;
    using System.Threading;
    using Microsoft.Owin.BuilderProperties;
    using Microsoft.ServiceBus.Messaging;
    using ReactiveArchitecture.Messaging;
    using ReactiveArchitecture.Messaging.Azure;

    public static class ServiceBusQueueMessagingExtensions
    {
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

            var queueClient = QueueClient.CreateFromConnectionString(connectionString, queueName);

            CancellationToken cancellationToken = new AppProperties(app.Properties).OnAppDisposing;

            var processor = new BrokeredMessageProcessor(
                messageHandler,
                messageSerializer,
                cancellationToken);

            queueClient.OnMessageAsync(processor.ProcessMessage);

            cancellationToken.Register(queueClient.Close);
        }
    }
}
