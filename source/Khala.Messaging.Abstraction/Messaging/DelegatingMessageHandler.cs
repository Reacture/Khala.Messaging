namespace Khala.Messaging
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// An <see cref="IMessageHandler"/> implementation that delegates the responsibility to handle messages to a function.
    /// </summary>
    public class DelegatingMessageHandler : IMessageHandler
    {
        private Func<Envelope, CancellationToken, Task> _handler;

        /// <summary>
        /// Initializes a new instance of the <see cref="DelegatingMessageHandler"/> class with a cancellable message handler function.
        /// </summary>
        /// <param name="handler">A cancellable message handler function.</param>
        public DelegatingMessageHandler(Func<Envelope, CancellationToken, Task> handler)
        {
            _handler = handler ?? throw new ArgumentNullException(nameof(handler));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DelegatingMessageHandler"/> class with a message handler function.
        /// </summary>
        /// <param name="handler">A message handler function.</param>
        public DelegatingMessageHandler(Func<Envelope, Task> handler)
        {
            if (handler == null)
            {
                throw new ArgumentNullException(nameof(handler));
            }

            _handler = (envelope, cancellationToken) => handler.Invoke(envelope);
        }

        /// <summary>
        /// Handles a message.
        /// </summary>
        /// <param name="envelope">An <see cref="Envelope"/> that contains the message object and related properties.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
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
