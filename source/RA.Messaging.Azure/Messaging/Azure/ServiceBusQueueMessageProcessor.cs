namespace ReactiveArchitecture.Messaging.Azure
{
    using System;
    using System.IO;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.ServiceBus.Messaging;

    public class ServiceBusQueueMessageProcessor
    {
        private readonly IMessageHandler _handler;
        private readonly IMessageSerializer _serializer;
        private readonly CancellationToken _cancellationToken;

        private ServiceBusQueueMessageProcessor(
            IMessageHandler handler,
            IMessageSerializer serializer,
            CancellationToken cancellationToken)
        {
            _handler = handler;
            _serializer = serializer;
            _cancellationToken = cancellationToken;
        }

        public static void Process(
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

            var processor = new ServiceBusQueueMessageProcessor(
                handler,
                serializer,
                cancellationToken);

            queueClient.OnMessageAsync(processor.ProcessMessage);

            cancellationToken.Register(queueClient.Close);
        }

        internal async Task ProcessMessage(BrokeredMessage brokeredMessage)
        {
            using (var stream = brokeredMessage.GetBody<Stream>())
            using (var memory = new MemoryStream())
            {
                await stream.CopyToAsync(memory, 81920, _cancellationToken).ConfigureAwait(false);
                byte[] bytes = memory.ToArray();
                string value = Encoding.UTF8.GetString(bytes);
                object message = _serializer.Deserialize(value);
                await _handler.Handle(message, _cancellationToken).ConfigureAwait(false);
            }

            await brokeredMessage.CompleteAsync();
        }
    }
}
