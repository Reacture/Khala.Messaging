namespace ReactiveArchitecture.Messaging.Azure
{
    using System;
    using System.Threading;
    using Microsoft.ServiceBus.Messaging;

    public static class ServiceBusQueueMessageProcessor
    {
        public static void Run(
            string connectionString,
            string queueName,
            IMessageHandler handler,
            IMessageSerializer serializer,
            CancellationToken cancellationToken)
        {
            if (connectionString == null)
            {
                throw new ArgumentNullException(nameof(connectionString));
            }

            if (queueName == null)
            {
                throw new ArgumentNullException(nameof(queueName));
            }

            if (handler == null)
            {
                throw new ArgumentNullException(nameof(handler));
            }

            if (serializer == null)
            {
                throw new ArgumentNullException(nameof(serializer));
            }

            var queueClient = QueueClient.CreateFromConnectionString(connectionString, queueName);

            var processor = new BrokeredMessageProcessor(
                handler,
                serializer,
                cancellationToken);

            queueClient.OnMessageAsync(processor.ProcessMessage);

            cancellationToken.Register(queueClient.Close);
        }
    }
}
