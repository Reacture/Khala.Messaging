namespace Khala.Messaging.Azure
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.ServiceBus.Messaging;

    public sealed class EventMessageProcessor : IEventProcessor
    {
        private readonly MessageProcessorCore<EventData> _processorCore;
        private readonly CancellationToken _cancellationToken;

        public EventMessageProcessor(
            MessageProcessorCore<EventData> processorCore,
            CancellationToken cancellationToken)
        {
            _processorCore = processorCore ?? throw new ArgumentNullException(nameof(processorCore));
            _cancellationToken = cancellationToken;
        }

        Task IEventProcessor.CloseAsync(PartitionContext context, CloseReason reason)
        {
            return Task.FromResult(true);
        }

        Task IEventProcessor.OpenAsync(PartitionContext context)
        {
            return Task.FromResult(true);
        }

        public Task ProcessEventsAsync(
            PartitionContext context, IEnumerable<EventData> messages)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (messages == null)
            {
                throw new ArgumentNullException(nameof(messages));
            }

            var eventDataList = new List<EventData>(messages);
            for (int i = 0; i < eventDataList.Count; i++)
            {
                if (eventDataList[i] == null)
                {
                    throw new ArgumentException(
                        $"{nameof(messages)} cannot contain null.",
                        nameof(messages));
                }
            }

            return ProcessEvents(context, eventDataList);
        }

        private async Task ProcessEvents(
            PartitionContext context, IEnumerable<EventData> messages)
        {
            foreach (EventData eventData in messages)
            {
                await ProcessEvent(context, eventData).ConfigureAwait(false);
            }
        }

        private Task ProcessEvent(PartitionContext context, EventData eventData)
            => _processorCore.Process(eventData, context.CheckpointAsync, _cancellationToken);
    }
}
