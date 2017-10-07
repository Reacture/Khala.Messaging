namespace Khala.Messaging.Azure
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.ServiceBus;
    using Microsoft.Azure.ServiceBus.Core;

    public sealed class ServiceBusMessageProcessor
    {
        private readonly Func<Message, Task> _acknowledge;
        private readonly MessageProcessor<Message> _processor;
        private readonly CancellationToken _cancellationToken;

        public ServiceBusMessageProcessor(
            IReceiverClient receiverClient,
            MessageProcessor<Message> processor,
            CancellationToken cancellationToken)
        {
            if (receiverClient == null)
            {
                throw new ArgumentNullException(nameof(receiverClient));
            }

            _acknowledge = MakeAcknowledge(receiverClient);
            _processor = processor ?? throw new ArgumentNullException(nameof(processor));
            _cancellationToken = cancellationToken;
        }

        private static Func<Message, Task> MakeAcknowledge(IReceiverClient receiverClient)
        {
            switch (receiverClient.ReceiveMode)
            {
                case ReceiveMode.PeekLock:
                    return message => receiverClient.CompleteAsync(message.SystemProperties.LockToken);

                case ReceiveMode.ReceiveAndDelete:
                default:
                    return message => Task.CompletedTask;
            }
        }

        public Task ProcessMessage(Message message, CancellationToken cancellationToken)
        {
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            return RunProcessMessage(message, cancellationToken);
        }

        private async Task RunProcessMessage(Message message, CancellationToken cancellationToken)
        {
            var context = new MessageContext<Message>(message, _acknowledge);
            using (var cancellation = CancellationTokenSource.CreateLinkedTokenSource(_cancellationToken, cancellationToken))
            {
                await _processor.Process(context, cancellation.Token);
            }
        }
    }
}
