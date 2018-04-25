namespace Khala.Messaging.Azure
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Provides the default implementation of <see cref="IEventMessageProcessor"/>.
    /// </summary>
    public class EventMessageProcessor : IEventMessageProcessor
    {
        private readonly IMessageHandler _messageHandler;

        /// <summary>
        /// Initializes a new instance of the <see cref="EventMessageProcessor"/> class.
        /// </summary>
        /// <param name="messageHandler">A message handler object.</param>
        public EventMessageProcessor(IMessageHandler messageHandler)
        {
            _messageHandler = messageHandler ?? throw new ArgumentNullException(nameof(messageHandler));
        }

        /// <inheritdoc/>
        public Task Process(
            Envelope envelope,
            IDictionary<string, object> properties,
            CancellationToken cancellationToken)
        {
            if (envelope == null)
            {
                throw new ArgumentNullException(nameof(envelope));
            }

            if (properties == null)
            {
                throw new ArgumentNullException(nameof(properties));
            }

            return _messageHandler.Accepts(envelope)
                ? _messageHandler.Handle(envelope, cancellationToken)
                : Task.CompletedTask;
        }
    }
}
