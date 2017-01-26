namespace ReactiveArchitecture.Messaging.Azure
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.ServiceBus.Messaging;

    public class EventMessageProcessor : IEventProcessor
    {
        private readonly IMessageHandler _messageHandler;
        private readonly IMessageSerializer _messageSerializer;
        private readonly IMessageProcessingExceptionHandler<EventData> _exceptionHandler;
        private readonly CancellationToken _cancellationToken;

        internal EventMessageProcessor(
            IMessageHandler messageHandler,
            IMessageSerializer messageSerializer,
            IMessageProcessingExceptionHandler<EventData> exceptionHandler,
            CancellationToken cancellationToken)
        {
            _messageHandler = messageHandler;
            _messageSerializer = messageSerializer;
            _exceptionHandler = exceptionHandler;
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
                await ProcessEvent(context, eventData).ConfigureAwait(false);
            }
        }

        private async Task ProcessEvent(
            PartitionContext context, EventData eventData)
        {
            byte[] body = null;
            Envelope envelope = null;

            try
            {
                body = eventData.GetBytes();

                string value = Encoding.UTF8.GetString(body);
                envelope = (Envelope)_messageSerializer.Deserialize(value);

                await _messageHandler
                    .Handle(envelope, _cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                var exceptionContext =
                    body == null ?
                    new MessageProcessingExceptionContext<EventData>(
                        eventData, exception)
                        :
                    envelope == null ?
                    new MessageProcessingExceptionContext<EventData>(
                        eventData, body, exception)
                        :
                    new MessageProcessingExceptionContext<EventData>(
                        eventData, body, envelope, exception);

                try
                {
                    await _exceptionHandler.Handle(exceptionContext);
                }
                catch (Exception exceptionHandlerError)
                {
                    Trace.TraceError(exceptionHandlerError.ToString());
                }

                if (exceptionContext.Handled)
                {
                    return;
                }

                throw;
            }

            await context.CheckpointAsync(eventData).ConfigureAwait(false);
        }
    }
}
