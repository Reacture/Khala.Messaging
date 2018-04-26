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

        internal Task Process(
            Envelope envelope,
            IDictionary<string, object> properties,
            CancellationToken cancellationToken)
        {
            if (_messageHandler.Accepts(envelope))
            {
                var context = new EventContext(envelope, properties);
                return ProcessAcceptedEvent(context, cancellationToken);
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Process a message.
        /// </summary>
        /// <param name="context">An <see cref="EventContext"/> object that contains an <see cref="Envelope"/> and a property bag from <see cref="EventData"/>.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <remarks>This method is not called by the framework for unacceptable message.</remarks>
        protected virtual Task ProcessAcceptedEvent(
            EventContext context,
            CancellationToken cancellationToken)
        {
            return _messageHandler.Handle(context.Envelope, cancellationToken);
        }
    }
}
