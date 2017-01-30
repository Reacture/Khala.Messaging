namespace Khala.Messaging.Azure
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.ServiceBus.Messaging;

    public class BrokeredMessageProcessor
    {
        private readonly IMessageHandler _messageHandler;
        private readonly IMessageSerializer _messageSerializer;
        private readonly CancellationToken _cancellationToken;
        private readonly IMessageProcessingExceptionHandler<BrokeredMessage> _exceptionHandler;

        public BrokeredMessageProcessor(
            IMessageHandler messageHandler,
            IMessageSerializer messageSerializer,
            IMessageProcessingExceptionHandler<BrokeredMessage> exceptionHandler,
            CancellationToken cancellationToken)
        {
            if (messageHandler == null)
            {
                throw new ArgumentNullException(nameof(messageHandler));
            }

            if (messageSerializer == null)
            {
                throw new ArgumentNullException(nameof(messageSerializer));
            }

            if (exceptionHandler == null)
            {
                throw new ArgumentNullException(nameof(exceptionHandler));
            }

            _messageHandler = messageHandler;
            _messageSerializer = messageSerializer;
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
            byte[] body = null;
            Envelope envelope = null;

            try
            {
                using (var stream = brokeredMessage.GetBody<Stream>())
                using (var memory = new MemoryStream())
                {
                    await stream
                        .CopyToAsync(memory, 81920, _cancellationToken)
                        .ConfigureAwait(false);
                    body = memory.ToArray();

                    string value = Encoding.UTF8.GetString(body);
                    envelope = (Envelope)_messageSerializer.Deserialize(value);

                    await _messageHandler
                        .Handle(envelope, _cancellationToken)
                        .ConfigureAwait(false);
                }
            }
            catch (Exception exception)
            {
                var exceptionContext =
                    body == null ?
                    new MessageProcessingExceptionContext<BrokeredMessage>(
                        brokeredMessage, exception)
                        :
                    envelope == null ?
                    new MessageProcessingExceptionContext<BrokeredMessage>(
                        brokeredMessage, body, exception)
                        :
                    new MessageProcessingExceptionContext<BrokeredMessage>(
                        brokeredMessage, body, envelope, exception);

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

            await brokeredMessage.CompleteAsync().ConfigureAwait(false);
        }
    }
}
