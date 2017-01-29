namespace Arcane.Messaging
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

        public Task Handle(
            Envelope envelope, CancellationToken cancellationToken)
        {
            if (envelope == null)
            {
                throw new ArgumentNullException(nameof(envelope));
            }

            return HandleMessage(envelope, cancellationToken);
        }

        private async Task HandleMessage(
            Envelope envelope, CancellationToken cancellationToken)
        {
            List<Exception> exceptions = null;

            foreach (IMessageHandler handler in _handlers)
            {
                try
                {
                    await handler.Handle(envelope, cancellationToken);
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
