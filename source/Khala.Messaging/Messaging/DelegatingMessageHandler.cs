namespace Khala.Messaging
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    public class DelegatingMessageHandler : IMessageHandler
    {
        private Func<Envelope, CancellationToken, Task> _func;

        public DelegatingMessageHandler(Func<Envelope, CancellationToken, Task> func)
        {
            _func = func ?? throw new ArgumentNullException(nameof(func));
        }

        public DelegatingMessageHandler(Func<Envelope, Task> func)
        {
            if (func == null)
            {
                throw new ArgumentNullException(nameof(func));
            }

            _func = (envelope, cancellationToken) => func.Invoke(envelope);
        }

        public Task Handle(Envelope envelope, CancellationToken cancellationToken)
        {
            if (envelope == null)
            {
                throw new ArgumentNullException(nameof(envelope));
            }

            return _func.Invoke(envelope, cancellationToken);
        }
    }
}
