namespace Khala.Messaging
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Provides a composite of message handlers.
    /// </summary>
    public sealed class CompositeMessageHandler : IMessageHandler
    {
        private readonly IEnumerable<IMessageHandler> _handlers;

        /// <summary>
        /// Initializes a new instance of the <see cref="CompositeMessageHandler"/> class.
        /// </summary>
        /// <param name="handlers">Message handlers</param>
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

        /// <summary>
        /// Gets inner message handlers.
        /// </summary>
        /// <value>
        /// Inner message handlers.
        /// </value>
        public IEnumerable<IMessageHandler> Handlers => _handlers;

        /// <inheritdoc/>
        public bool Accepts(Envelope envelope)
        {
            if (envelope == null)
            {
                throw new ArgumentNullException(nameof(envelope));
            }

            foreach (IMessageHandler handler in Handlers)
            {
                if (handler.Accepts(envelope))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Handles a message.
        /// </summary>
        /// <param name="envelope">An <see cref="Envelope"/> that contains the message object and related properties.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <remarks>Even if some message handler fails, <paramref name="envelope"/> is sent to all message handlers.</remarks>
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
                    await handler.Handle(envelope, cancellationToken).ConfigureAwait(false);
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
