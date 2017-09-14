namespace Khala.Messaging
{
    using System;
    using System.Threading.Tasks;

    public class DelegatingMessageBusExceptionHandler : IMessageBusExceptionHandler
    {
        private Func<MessageBusExceptionContext, Task> _func;

        public DelegatingMessageBusExceptionHandler(Func<MessageBusExceptionContext, Task> func)
        {
            _func = func ?? throw new ArgumentNullException(nameof(func));
        }

        public Task Handle(MessageBusExceptionContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            return _func.Invoke(context);
        }
    }
}
