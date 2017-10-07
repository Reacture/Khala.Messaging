namespace Khala.Messaging
{
    using System;
    using System.Threading.Tasks;

    public sealed class DelegatingMessageProcessingExceptionHandler<TData> :
        IMessageProcessingExceptionHandler<TData>
        where TData : class
    {
        private readonly Func<MessageProcessingExceptionContext<TData>, Task> _handler;

        public DelegatingMessageProcessingExceptionHandler(
            Func<MessageProcessingExceptionContext<TData>, Task> handler)
        {
            _handler = handler ?? throw new ArgumentNullException(nameof(handler));
        }

        public Task Handle(
            MessageProcessingExceptionContext<TData> exceptionContext)
        {
            if (exceptionContext == null)
            {
                throw new ArgumentNullException(nameof(exceptionContext));
            }

            return _handler.Invoke(exceptionContext);
        }
    }
}
