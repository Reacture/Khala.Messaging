namespace Khala.Messaging.Azure
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.EventHubs;
    using Microsoft.Azure.EventHubs.Processor;

    public sealed class EventProcessorFactory : IEventProcessorFactory
    {
        private readonly MessageProcessor<EventData> _processor;
        private readonly CancellationToken _cancellationToken;

        public EventProcessorFactory(MessageProcessor<EventData> processor, CancellationToken cancellationToken)
        {
            _processor = processor ?? throw new ArgumentNullException(nameof(processor));
            _cancellationToken = cancellationToken;
        }

        public IEventProcessor CreateEventProcessor(PartitionContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var partitionClient = new PartitionClient(context);
            return new ProcessorAdapter(new EventHubMessageProcessor(partitionClient, _processor, _cancellationToken));
        }

        private class PartitionClient : IPartitionClient
        {
            private readonly PartitionContext _context;

            public PartitionClient(PartitionContext context) => _context = context;

            public Task Checkpoint(EventData eventData) => _context.CheckpointAsync(eventData);
        }

        private class ProcessorAdapter : IEventProcessor
        {
            private readonly EventHubMessageProcessor _processor;

            public ProcessorAdapter(EventHubMessageProcessor processor)
            {
                _processor = processor;
            }

            public Task CloseAsync(PartitionContext context, CloseReason reason)
            {
                return Task.CompletedTask;
            }

            public Task OpenAsync(PartitionContext context)
            {
                return Task.CompletedTask;
            }

            public Task ProcessErrorAsync(PartitionContext context, Exception error)
            {
                return Task.CompletedTask;
            }

            public Task ProcessEventsAsync(PartitionContext context, IEnumerable<EventData> messages)
            {
                return _processor.ProcessMessages(messages);
            }
        }
    }
}
