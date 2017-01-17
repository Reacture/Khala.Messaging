namespace ReactiveArchitecture.Messaging
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Threading;
    using System.Threading.Tasks;

    public class CompositeMessageHandler : IMessageHandler
    {
        private readonly IEnumerable<IMessageHandler> _handlers;

        public CompositeMessageHandler(params IMessageHandler[] handlers)
        {
            if (handlers == null)
            {
                throw new ArgumentNullException(nameof(handlers));
            }

            var handlerList = new List<IMessageHandler>(handlers);
            for (int i = 0; i < handlerList.Count; i++)
            {
                if (handlerList[i] == null)
                {
                    throw new ArgumentException(
                        $"{nameof(handlers)} cannot contain null.",
                        nameof(handlers));
                }
            }

            _handlers = new ReadOnlyCollection<IMessageHandler>(handlerList);
        }

        async Task IMessageHandler.Handle(
            object message,
            CancellationToken cancellationToken)
        {
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            List<Exception> exceptions = null;

            foreach (IMessageHandler handler in _handlers)
            {
                try
                {
                    await handler.Handle(message, cancellationToken);
                }
                catch (Exception exception)
                {
                    if (exceptions == null)
                    {
                        exceptions = new List<Exception>();
                    }

                    exceptions.Add(exception);
                }
            }

            if (exceptions != null)
            {
                throw new AggregateException(exceptions);
            }
        }
    }
}
