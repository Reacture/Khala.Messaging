namespace ReactiveArchitecture.Messaging.Azure
{
    using System;
    using System.IO;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.ServiceBus.Messaging;

    public class BrokeredMessageProcessor
    {
        private readonly IMessageHandler _handler;
        private readonly IMessageSerializer _serializer;
        private readonly CancellationToken _cancellationToken;

        public BrokeredMessageProcessor(
            IMessageHandler handler,
            IMessageSerializer serializer,
            CancellationToken cancellationToken)
        {
            if (handler == null)
            {
                throw new ArgumentNullException(nameof(handler));
            }

            if (serializer == null)
            {
                throw new ArgumentNullException(nameof(serializer));
            }

            _handler = handler;
            _serializer = serializer;
            _cancellationToken = cancellationToken;
        }

        public Task ProcessMessage(BrokeredMessage brokeredMessage)
        {
            if (brokeredMessage == null)
            {
                throw new ArgumentNullException(nameof(brokeredMessage));
            }

            return Process(brokeredMessage);
        }

        private async Task Process(BrokeredMessage brokeredMessage)
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
