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
#pragma warning disable IDE0034 // Disable warning because it changes the type to Guid from Guid?.
                    Guid.TryParse(message.CorrelationId, out Guid correlationId) ? correlationId : default(Guid?),
#pragma warning restore IDE0034 // Disable warning because it changes the type to Guid from Guid?.
                    serializer.Deserialize(Encoding.UTF8.GetString(message.Body)));

                await messageBus.Send(envelope);

                await queueClient.CompleteAsync(message.SystemProperties.LockToken);
            }

            Task HandleException(ExceptionReceivedEventArgs exceptionReceived) => Task.CompletedTask;

            return queueClient.CloseAsync;
        }
    }
}
