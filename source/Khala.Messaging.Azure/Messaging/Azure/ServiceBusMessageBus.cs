namespace Khala.Messaging.Azure
{
    using System;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.ServiceBus;
    using Microsoft.Azure.ServiceBus.Core;

    /// <summary>
    /// Provides an Azure Service Bus based implementation of <see cref="IScheduledMessageBus"/> interfaces.
    /// </summary>
    public class ServiceBusMessageBus : IScheduledMessageBus
    {
        private readonly MessageSender _sender;
        private readonly IMessageSerializer _serializer;

        /// <summary>
        /// Initializes a new instance of the <see cref="ServiceBusMessageBus"/> class.
        /// </summary>
        /// <param name="connectionStringBuilder">A <see cref="ServiceBusConnectionStringBuilder"/>to build an Azure Service Bus connection string.</param>
        /// <param name="serializer">A serializer to serialize messages.</param>
        public ServiceBusMessageBus(
            ServiceBusConnectionStringBuilder connectionStringBuilder,
            IMessageSerializer serializer)
        {
            if (connectionStringBuilder == null)
            {
                throw new ArgumentNullException(nameof(connectionStringBuilder));
            }

            _sender = new MessageSender(connectionStringBuilder);
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
        }

        /// <summary>
        /// Sends an enveloped message to the Azure Service Bus entity at the scheduled time.
        /// </summary>
        /// <param name="envelope">A scheduled envelope message to be sent.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public Task Send(ScheduledEnvelope envelope, CancellationToken cancellationToken)
        {
            if (envelope == null)
            {
                throw new ArgumentNullException(nameof(envelope));
            }

            return _sender.SendAsync(new Message
            {
                Body = Encoding.UTF8.GetBytes(_serializer.Serialize(envelope.Envelope.Message)),
                ScheduledEnqueueTimeUtc = envelope.ScheduledTime.UtcDateTime,
                MessageId = envelope.Envelope.MessageId.ToString("n"),
                CorrelationId = envelope.Envelope.CorrelationId?.ToString("n")
            });
        }

        /// <summary>
        /// Releases resources that used to access the Azure Service Bus entity.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        public Task Close() => _sender.CloseAsync();
    }
}
