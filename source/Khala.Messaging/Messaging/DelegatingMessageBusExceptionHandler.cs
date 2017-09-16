namespace Khala.Messaging
{
    using System;
    using System.Threading.Tasks;

    public class DelegatingMessageBusExceptionHandler : IMessageBusExceptionHandler
    {
        private Func<MessageBusExceptionContext, Task> _handler;

        public DelegatingMessageBusExceptionHandler(Func<MessageBusExceptionContext, Task> handler)
        {
            _handler = handler ?? throw new ArgumentNullException(nameof(handler));
        }

        public Task Handle(MessageBusExceptionContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            return _handler.Invoke(context);
        }
    }
}
