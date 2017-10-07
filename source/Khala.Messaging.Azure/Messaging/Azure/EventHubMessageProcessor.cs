namespace Khala.Messaging.Azure
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.EventHubs;

    public class EventHubMessageProcessor
    {
        private readonly Func<EventData, Task> _acknowledge;
        private readonly MessageProcessor<EventData> _processor;
        private readonly CancellationToken _cancellationToken;

        public EventHubMessageProcessor(
            IPartitionClient partitionClient,
            MessageProcessor<EventData> processor,
            CancellationToken cancellationToken)
        {
            if (partitionClient == null)
            {
                throw new ArgumentNullException(nameof(partitionClient));
            }

            _acknowledge = partitionClient.Checkpoint;
            _processor = processor ?? throw new ArgumentNullException(nameof(processor));
            _cancellationToken = cancellationToken;
        }

        public Task ProcessMessages(IEnumerable<EventData> messages)
        {
            if (messages == null)
            {
                throw new ArgumentNullException(nameof(messages));
            }

            return RunProcessMessages(messages);
        }

        private async Task RunProcessMessages(IEnumerable<EventData> messages)
        {
            foreach (EventData data in messages)
            {
                var context = new MessageContext<EventData>(data, _acknowledge);
                await _processor.Process(context, _cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
