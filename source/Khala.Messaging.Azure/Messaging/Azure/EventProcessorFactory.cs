namespace Khala.Messaging.Azure
{
    using System;
    using System.Threading;
    using Microsoft.Azure.EventHubs.Processor;

    /// <summary>
    /// Generates <see cref="IEventProcessor"/> implementors that routes messages to a message handler.
    /// </summary>
    public sealed class EventProcessorFactory : IEventProcessorFactory
    {
        private readonly IMessageHandler _messageHandler;
        private readonly IEventProcessingExceptionHandler _exceptionHandler;
        private readonly CancellationToken _cancellationToken;

        /// <summary>
        /// Initializes a new instance of the <see cref="EventProcessorFactory"/> class.
        /// </summary>
        /// <param name="messageHandler">A message handler object.</param>
        /// <param name="exceptionHandler">An exception handler object.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while event processors working.</param>
        public EventProcessorFactory(
            IMessageHandler messageHandler,
            IEventProcessingExceptionHandler exceptionHandler,
            CancellationToken cancellationToken)
        {
            _messageHandler = messageHandler ?? throw new ArgumentNullException(nameof(messageHandler));
            _exceptionHandler = exceptionHandler ?? throw new ArgumentNullException(nameof(exceptionHandler));
            _cancellationToken = cancellationToken;
        }

        /// <summary>
        /// Creates an instance of <see cref="IEventProcessor"/> that routes messages to the <see cref="IMessageHandler"/>.
        /// </summary>
        /// <param name="context">A <see cref="PartitionContext"/>.</param>
        /// <returns>An instance of <see cref="IEventProcessor"/>.</returns>
        public IEventProcessor CreateEventProcessor(PartitionContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            return new EventProcessor(_messageHandler, _exceptionHandler, _cancellationToken);
        }
    }
}
