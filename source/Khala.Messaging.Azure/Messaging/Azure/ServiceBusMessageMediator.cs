namespace Khala.Messaging.Azure
{
    using System;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.ServiceBus;

    /// <summary>
    /// Provides function to brokers messages from an Azure Service Bus queue to a message bus.
    /// </summary>
    public static class ServiceBusMessageMediator
    {
        /// <summary>
        /// Start to broker messages from an Azure Service bus queue to a message bus.
        /// </summary>
        /// <param name="connectionStringBuilder">A <see cref="ServiceBusConnectionStringBuilder"/>to build an Azure Service Bus connection string.</param>
        /// <param name="messageBus">A message bus client.</param>
        /// <param name="serializer">A serializer to serialize messages.</param>
        /// <returns>The asynchronous function to release resources that used to access the Azure Service Bus queue.</returns>
        public static Func<Task> Start(
            ServiceBusConnectionStringBuilder connectionStringBuilder,
            IMessageBus messageBus,
            IMessageSerializer serializer)
        {
            if (connectionStringBuilder == null)
            {
                throw new ArgumentNullException(nameof(connectionStringBuilder));
            }

            if (messageBus == null)
            {
                throw new ArgumentNullException(nameof(messageBus));
            }

            if (serializer == null)
            {
                throw new ArgumentNullException(nameof(serializer));
            }

            var queueClient = new QueueClient(connectionStringBuilder, ReceiveMode.PeekLock);
            queueClient.RegisterMessageHandler(HandleMessage, new MessageHandlerOptions(HandleException));

            async Task HandleMessage(Message message, CancellationToken cancellationToken)
            {
                var envelope = new Envelope(
                    Guid.Parse(message.MessageId),
                    serializer.Deserialize(Encoding.UTF8.GetString(message.Body)),
                    GetOperationId(message),
                    GetCorrelationId(message),
                    GetContributor(message));

                await messageBus.Send(envelope).ConfigureAwait(false);

                await queueClient.CompleteAsync(message.SystemProperties.LockToken).ConfigureAwait(false);
            }

            Task HandleException(ExceptionReceivedEventArgs exceptionReceived) => Task.CompletedTask;

            return queueClient.CloseAsync;
        }

        private static string GetOperationId(Message message)
            => message.UserProperties.TryGetValue("Khala.Messaging.Envelope.OperationId", out object value)
            ? value as string
            : default;

        private static Guid? GetCorrelationId(Message message)
            => Guid.TryParse(message.CorrelationId, out Guid correlationId)
            ? correlationId
            : default(Guid?);

        private static string GetContributor(Message message)
            => message.UserProperties.TryGetValue("Khala.Messaging.Envelope.Contributor", out object value)
            ? value as string
            : default;
    }
}
