namespace Khala.Messaging
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    public class MessageProcessor<TData>
        where TData : class
    {
        private readonly IMessageDataSerializer<TData> _serializer;
        private readonly IMessageHandler _messageHandler;
        private readonly IMessageProcessingExceptionHandler<TData> _exceptionHandler;

        public MessageProcessor(
            IMessageDataSerializer<TData> serializer,
            IMessageHandler messageHandler,
            IMessageProcessingExceptionHandler<TData> exceptionHandler)
        {
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            _messageHandler = messageHandler ?? throw new ArgumentNullException(nameof(messageHandler));
            _exceptionHandler = exceptionHandler ?? throw new ArgumentNullException(nameof(exceptionHandler));
        }

        public Task Process(IMessageContext<TData> context, CancellationToken cancellationToken)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            return RunProcess(context, cancellationToken);
        }

        private async Task RunProcess(IMessageContext<TData> context, CancellationToken cancellationToken)
        {
            TData data = context.Data;
            Envelope envelope = null;
            try
            {
                envelope = await _serializer.Deserialize(data).ConfigureAwait(false);
                await _messageHandler.Handle(envelope, cancellationToken).ConfigureAwait(false);
                await context.Acknowledge().ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                if (false == await TryHandleException(data, envelope, exception).ConfigureAwait(false))
                {
                    throw;
                }
            }
        }

        private async Task<bool> TryHandleException(TData data, Envelope envelope, Exception exception)
        {
            MessageProcessingExceptionContext<TData> exceptionContext;
            exceptionContext = CreateExceptionContext(data, envelope, exception);
            await InvokeExceptionHandlerGenerously(exceptionContext).ConfigureAwait(false);
            return exceptionContext.Handled;
        }

        private static MessageProcessingExceptionContext<TData> CreateExceptionContext(
            TData data,
            Envelope envelope,
            Exception exception)
        {
            return envelope == null
                ? new MessageProcessingExceptionContext<TData>(data, exception)
                : new MessageProcessingExceptionContext<TData>(data, envelope, exception);
        }

        private async Task InvokeExceptionHandlerGenerously(
            MessageProcessingExceptionContext<TData> exceptionContext)
        {
            try
            {
                await _exceptionHandler.Handle(exceptionContext).ConfigureAwait(false);
            }
            catch
            {
            }
        }
    }
}
