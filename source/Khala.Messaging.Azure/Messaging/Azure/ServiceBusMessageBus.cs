namespace Khala.Messaging.Azure
{
    using System;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.ServiceBus;
    using Microsoft.Azure.ServiceBus.Core;

    public class ServiceBusMessageBus : IScheduledMessageBus
    {
        private readonly MessageSender _sender;
        private readonly IMessageSerializer _serializer;

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

        public Task Close() => _sender.CloseAsync();
    }
}
