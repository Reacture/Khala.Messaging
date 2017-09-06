namespace Khala.Messaging.Azure
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.ServiceBus.Messaging;

    public sealed class EventMessageProcessor : IEventProcessor
    {
        private readonly IMessageDataSerializer<EventData> _serializer;
        private readonly IMessageHandler _messageHandler;
        private readonly IMessageProcessingExceptionHandler<EventData> _exceptionHandler;
        private readonly CancellationToken _cancellationToken;

        internal EventMessageProcessor(
            IMessageDataSerializer<EventData> serializer,
            IMessageHandler messageHandler,
            IMessageProcessingExceptionHandler<EventData> exceptionHandler,
            CancellationToken cancellationToken)
        {
            _serializer = serializer;
            _messageHandler = messageHandler;
            _exceptionHandler = exceptionHandler;
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

        private async Task ProcessEvent(
            PartitionContext context, EventData eventData)
        {
            Envelope envelope = null;
            try
            {
                envelope = await _serializer.Deserialize(eventData).ConfigureAwait(false);
                await _messageHandler.Handle(envelope, _cancellationToken).ConfigureAwait(false);
                await context.CheckpointAsync(eventData).ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                try
                {
                    var exceptionContext = envelope == null
                        ? new MessageProcessingExceptionContext<EventData>(eventData, exception)
                        : new MessageProcessingExceptionContext<EventData>(eventData, envelope, exception);

                    await _exceptionHandler.Handle(exceptionContext).ConfigureAwait(false);

                    if (exceptionContext.Handled)
                    {
                        return;
                    }
                }
                catch (Exception unhandleable)
                {
                    Trace.TraceError(unhandleable.ToString());
                }

                throw;
            }
        }
    }
}
