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
        private readonly EventMessageProcessor _processor;
        private readonly IEventProcessingExceptionHandler _exceptionHandler;
        private readonly CancellationToken _cancellationToken;

        public EventProcessor(
            EventMessageProcessor processor,
            IEventProcessingExceptionHandler exceptionHandler,
            CancellationToken cancellationToken)
        {
            _serializer = new EventDataSerializer();
            _processor = processor;
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
                await ProcessEvent(eventData).ConfigureAwait(false);
                await context.CheckpointAsync(eventData).ConfigureAwait(false);
            }
        }

        private async Task ProcessEvent(EventData eventData)
        {
            Envelope envelope = null;
            try
            {
                envelope = _serializer.Deserialize(eventData);
                IDictionary<string, object> properties = eventData.Properties;
                await _processor.Process(envelope, properties, _cancellationToken).ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                await HandleExceptionFaultTolerantly(eventData, envelope, exception).ConfigureAwait(false);
                if (exception is TaskCanceledException)
                {
                    throw;
                }
            }
        }

        private async Task HandleExceptionFaultTolerantly(EventData eventData, Envelope envelope, Exception exception)
        {
            try
            {
                EventProcessingExceptionContext context = envelope == null
                    ? new EventProcessingExceptionContext(eventData, exception)
                    : new EventProcessingExceptionContext(eventData, envelope, exception);
                await _exceptionHandler.Handle(context).ConfigureAwait(false);
            }
            catch
            {
            }
        }
    }
}
