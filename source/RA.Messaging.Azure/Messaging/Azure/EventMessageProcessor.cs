namespace ReactiveArchitecture.Messaging.Azure
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.ServiceBus.Messaging;

    public class EventMessageProcessor : IEventProcessor
    {
        private readonly IMessageHandler _handler;
        private readonly IMessageSerializer _serializer;
        private readonly CancellationToken _cancellationToken;

        public EventMessageProcessor(
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

        public Task CloseAsync(PartitionContext context, CloseReason reason)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            return Task.FromResult(true);
        }

        public Task OpenAsync(PartitionContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

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
                await ProcessEvent(eventData).ConfigureAwait(false);
                await context.CheckpointAsync(eventData).ConfigureAwait(false);
            }
        }

        private Task ProcessEvent(EventData eventData)
        {
            byte[] bytes = eventData.GetBytes();
            string value = Encoding.UTF8.GetString(bytes);
            object message = _serializer.Deserialize(value);
            return _handler.Handle(message, _cancellationToken);
        }
    }
}
