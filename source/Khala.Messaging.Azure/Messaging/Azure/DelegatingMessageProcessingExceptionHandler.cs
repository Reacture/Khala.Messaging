namespace Khala.Messaging.Azure
{
    using System;
    using System.Threading.Tasks;

    public sealed class DelegatingMessageProcessingExceptionHandler<TSource> :
        IMessageProcessingExceptionHandler<TSource>
        where TSource : class
    {
        private readonly Func<MessageProcessingExceptionContext<TSource>, Task> _handler;

        public DelegatingMessageProcessingExceptionHandler(
            Func<MessageProcessingExceptionContext<TSource>, Task> handler)
        {
            _handler = handler ?? throw new ArgumentNullException(nameof(handler));
        }

        public Task Handle(MessageProcessingExceptionContext<TSource> context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            return InvokeHandler(context);
        }

        private async Task InvokeHandler(MessageProcessingExceptionContext<TSource> context)
        {
            try
            {
                await _handler.Invoke(context);
            }
            catch
            {
            }
        }
    }
}
