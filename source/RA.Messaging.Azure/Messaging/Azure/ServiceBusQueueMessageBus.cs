namespace ReactiveArchitecture.Messaging.Azure
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.ServiceBus.Messaging;

    public class ServiceBusQueueMessageBus : IMessageBus
    {
        private readonly QueueClient _queueClient;
        private readonly IMessageSerializer _serializer;

        public ServiceBusQueueMessageBus(
            QueueClient queueClient, IMessageSerializer serializer)
        {
            if (queueClient == null)
            {
                throw new ArgumentNullException(nameof(queueClient));
            }

            if (serializer == null)
            {
                throw new ArgumentNullException(nameof(serializer));
            }

            _queueClient = queueClient;
            _serializer = serializer;
        }

        public Task Send(
            object message,
            CancellationToken cancellationToken)
        {
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            BrokeredMessage brokeredMessage = GetBrokeredMessage(message);
            return _queueClient.SendAsync(brokeredMessage);
        }

        public Task SendBatch(
            IEnumerable<object> messages,
            CancellationToken cancellationToken)
        {
            if (messages == null)
            {
                throw new ArgumentNullException(nameof(messages));
            }

            var brokeredMessages = new List<BrokeredMessage>();

            foreach (object message in messages)
            {
                if (message == null)
                {
                    throw new ArgumentException(
                        $"{nameof(messages)} cannot contain null.",
                        nameof(messages));
                }

                brokeredMessages.Add(GetBrokeredMessage(message));
            }

            return _queueClient.SendBatchAsync(brokeredMessages);
        }

        private BrokeredMessage GetBrokeredMessage(object message)
        {
            string data = _serializer.Serialize(message);
            byte[] bytes = Encoding.UTF8.GetBytes(data);
            var brokeredMessage = new BrokeredMessage(
                new MemoryStream(bytes), ownsStream: true);

            var partitioned = message as IPartitioned;
            if (partitioned != null)
            {
                brokeredMessage.PartitionKey = partitioned.PartitionKey;
            }

            return brokeredMessage;
        }
    }
}
