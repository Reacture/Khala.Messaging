namespace Khala.Messaging.Azure
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.EventHubs;
    using Microsoft.Azure.EventHubs.Processor;

    internal class EventProcessor : IEventProcessor
    {
        private readonly EventDataSerializer _serializer;
        private readonly IMessageHandler _messageHandler;
        private readonly IEventProcessingExceptionHandler _exceptionHandler;
        private readonly CancellationToken _cancellationToken;

        public EventProcessor(
            IMessageHandler messageHandler,
            IEventProcessingExceptionHandler exceptionHandler,
            CancellationToken cancellationToken)
        {
            _serializer = new EventDataSerializer();
            _messageHandler = messageHandler;
            _exceptionHandler = exceptionHandler;
            _cancellationToken = cancellationToken;
        }

        public Task CloseAsync(PartitionContext context, CloseReason reason) => Task.CompletedTask;

        public Task OpenAsync(PartitionContext context) => Task.CompletedTask;

        public Task ProcessErrorAsync(PartitionContext context, Exception error) => Task.CompletedTask;

        public async Task ProcessEventsAsync(PartitionContext context, IEnumerable<EventData> messages)
        {
            foreach (EventData eventData in messages)
            {
                Envelope envelope = null;
                try
                {
                    envelope = _serializer.Deserialize(eventData);
                    await _messageHandler.Handle(envelope, _cancellationToken);
                }
                catch (Exception exception)
                {
                    await HandleExceptionFaultTolerantly(eventData, envelope, exception);

                    if (exception is TaskCanceledException)
                    {
                        throw;
                    }
                }

                await context.CheckpointAsync(eventData);
            }
        }

        private async Task HandleExceptionFaultTolerantly(EventData eventData, Envelope envelope, Exception exception)
        {
            try
            {
                var context = envelope == null
                    ? new EventProcessingExceptionContext(eventData, exception)
                    : new EventProcessingExceptionContext(eventData, envelope, exception);
                await _exceptionHandler.Handle(context);
            }
            catch
            {
            }
        }
    }
}
