namespace Khala.Messaging.Azure
{
    using System;
    using System.Diagnostics;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.ServiceBus.Messaging;

    public sealed class BrokeredMessageProcessor
    {
        private readonly BrokeredMessageSerializer _serializer;
        private readonly IMessageHandler _messageHandler;
        private readonly IMessageProcessingExceptionHandler<BrokeredMessage> _exceptionHandler;
        private readonly CancellationToken _cancellationToken;

        public BrokeredMessageProcessor(
            BrokeredMessageSerializer serializer,
            IMessageHandler messageHandler,
            IMessageProcessingExceptionHandler<BrokeredMessage> exceptionHandler,
            CancellationToken cancellationToken)
        {
            if (serializer == null)
            {
                throw new ArgumentNullException(nameof(serializer));
            }

            if (messageHandler == null)
            {
                throw new ArgumentNullException(nameof(messageHandler));
            }

            if (exceptionHandler == null)
            {
                throw new ArgumentNullException(nameof(exceptionHandler));
            }

            _serializer = serializer;
            _messageHandler = messageHandler;
            _exceptionHandler = exceptionHandler;
            _cancellationToken = cancellationToken;
        }

        public Task ProcessMessage(BrokeredMessage brokeredMessage)
        {
            if (brokeredMessage == null)
            {
                throw new ArgumentNullException(nameof(brokeredMessage));
            }

            return Process(brokeredMessage);
        }

        private async Task Process(BrokeredMessage brokeredMessage)
        {
            Envelope envelope = null;
            try
            {
                envelope = await _serializer.Deserialize(brokeredMessage).ConfigureAwait(false);
                await _messageHandler.Handle(envelope, _cancellationToken).ConfigureAwait(false);
                await brokeredMessage.CompleteAsync().ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                try
                {
                    var exceptionContext = envelope == null
                        ? new MessageProcessingExceptionContext<BrokeredMessage>(brokeredMessage, exception)
                        : new MessageProcessingExceptionContext<BrokeredMessage>(brokeredMessage, envelope, exception);

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
