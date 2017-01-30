namespace Khala.Messaging.Azure
{
    using System;
    using System.Diagnostics;
    using System.Threading.Tasks;

    public class DelegatingMessageProcessingExceptionHandler<TSource> :
        IMessageProcessingExceptionHandler<TSource>
        where TSource : class
    {
        private readonly Func<MessageProcessingExceptionContext<TSource>, Task> _handler;

        public DelegatingMessageProcessingExceptionHandler(
            Func<MessageProcessingExceptionContext<TSource>, Task> handler)
        {
            if (handler == null)
            {
                throw new ArgumentNullException(nameof(handler));
            }

            _handler = handler;
        }

        public Task Handle(MessageProcessingExceptionContext<TSource> context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            return InvokeHandler(context);
        }

        private async Task InvokeHandler(
            MessageProcessingExceptionContext<TSource> context)
        {
            try
            {
                await _handler.Invoke(context);
            }
            catch (Exception handlerError)
            {
                Trace.TraceError(handlerError.ToString());
            }
        }
    }
}
