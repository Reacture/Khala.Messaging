namespace Khala.Messaging
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    public class DelegatingMessageHandler : IMessageHandler
    {
        private Func<Envelope, CancellationToken, Task> _handler;

        public DelegatingMessageHandler(Func<Envelope, CancellationToken, Task> handler)
        {
            _handler = handler ?? throw new ArgumentNullException(nameof(handler));
        }

        public DelegatingMessageHandler(Func<Envelope, Task> handler)
        {
            if (handler == null)
            {
                throw new ArgumentNullException(nameof(handler));
            }

            _handler = (envelope, cancellationToken) => handler.Invoke(envelope);
        }

        public Task Handle(Envelope envelope, CancellationToken cancellationToken)
        {
            if (envelope == null)
            {
                throw new ArgumentNullException(nameof(envelope));
            }

            return _handler.Invoke(envelope, cancellationToken);
        }
    }
}
