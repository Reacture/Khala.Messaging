namespace Khala.Messaging.Azure
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.ServiceBus;
    using Microsoft.Azure.ServiceBus.Core;

    public sealed class ServiceBusMessageProcessor
    {
        private readonly IReceiverClient _receiverClient;
        private readonly MessageProcessorCore<Message> _processorCore;
        private readonly CancellationToken _cancellationToken;

        public ServiceBusMessageProcessor(
            IReceiverClient receiverClient,
            MessageProcessorCore<Message> processorCore,
            CancellationToken cancellationToken)
        {
            _receiverClient = receiverClient;
            _processorCore = processorCore;
            _cancellationToken = cancellationToken;
        }

        public Task ProcessMessage(Message message)
        {
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            return _processorCore.Process(message, Complete, _cancellationToken);
        }

        private Task Complete(Message message)
            => _receiverClient.CompleteAsync(message.SystemProperties.LockToken);
    }
}
