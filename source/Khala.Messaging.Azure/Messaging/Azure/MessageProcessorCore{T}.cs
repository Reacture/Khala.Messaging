namespace Khala.Messaging.Azure
{
    using System;
    using System.Diagnostics;
    using System.Threading;
    using System.Threading.Tasks;

    public class MessageProcessorCore<T>
        where T : class
    {
        private readonly IMessageHandler _messageHandler;
        private readonly IMessageProcessingExceptionHandler<T> _exceptionHandler;

        public MessageProcessorCore(
            IMessageHandler messageHandler,
            IMessageProcessingExceptionHandler<T> exceptionHandler)
        {
            _messageHandler = messageHandler ?? throw new ArgumentNullException(nameof(messageHandler));
            _exceptionHandler = exceptionHandler ?? throw new ArgumentNullException(nameof(exceptionHandler));
        }

        public Task Process(
            T source,
            Func<T, Task<Envelope>> deserialize,
            Func<T, Task> checkpoint,
            CancellationToken cancellationToken)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (deserialize == null)
            {
                throw new ArgumentNullException(nameof(deserialize));
            }

            if (checkpoint == null)
            {
                throw new ArgumentNullException(nameof(checkpoint));
            }

            async Task Run()
            {
                Envelope envelope = null;
                try
                {
                    envelope = await deserialize.Invoke(source).ConfigureAwait(false);
                    await _messageHandler.Handle(envelope, cancellationToken).ConfigureAwait(false);
                    await checkpoint.Invoke(source).ConfigureAwait(false);
                }
                catch (Exception exception)
                {
                    try
                    {
                        var exceptionContext
                            = envelope == null
                            ? new MessageProcessingExceptionContext<T>(source, exception)
                            : new MessageProcessingExceptionContext<T>(source, envelope, exception);

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

            return Run();
        }
    }
}
