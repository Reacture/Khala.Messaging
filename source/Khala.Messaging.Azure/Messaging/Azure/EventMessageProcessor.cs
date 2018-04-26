namespace Khala.Messaging.Azure
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.EventHubs;

    /// <summary>
    /// Provides an event message processor that processes message from EventHub.
    /// </summary>
    public class EventMessageProcessor
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

        internal IMessageHandler MessageHandler => _messageHandler;

        /// <summary>
        /// Process a message.
        /// </summary>
        /// <param name="envelope">An <see cref="Envelope"/> that contains the message object and related properties.</param>
        /// <param name="properties">A property bag from <see cref="EventData"/>.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public virtual Task Process(
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

            return _messageHandler.Handle(envelope, cancellationToken);
        }
    }
}
