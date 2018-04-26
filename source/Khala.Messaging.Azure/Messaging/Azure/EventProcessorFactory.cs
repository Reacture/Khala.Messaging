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
        private readonly EventMessageProcessor _messageProcessor;
        private readonly IEventProcessingExceptionHandler _exceptionHandler;
        private readonly CancellationToken _cancellationToken;

        /// <summary>
        /// Initializes a new instance of the <see cref="EventProcessorFactory"/> class.
        /// </summary>
        /// <param name="messageProcessor">An <see cref="EventMessageProcessor"/>.</param>
        /// <param name="exceptionHandler">An exception handler object.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while event processors working.</param>
        public EventProcessorFactory(
            EventMessageProcessor messageProcessor,
            IEventProcessingExceptionHandler exceptionHandler,
            CancellationToken cancellationToken)
        {
            _messageProcessor = messageProcessor ?? throw new ArgumentNullException(nameof(messageProcessor));
            _exceptionHandler = exceptionHandler ?? throw new ArgumentNullException(nameof(exceptionHandler));
            _cancellationToken = cancellationToken;
        }

#pragma warning disable SA1642 // Constructor summary documentation must begin with standard text
        /// <summary>
        /// This constructor is obsolete. Use <see cref="EventProcessorFactory(EventMessageProcessor, IEventProcessingExceptionHandler, CancellationToken)"/> instead.
        /// </summary>
        /// <param name="messageHandler">A message handler object.</param>
        /// <param name="exceptionHandler">An exception handler object.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while event processors working.</param>
        [Obsolete("Use EventProcessorFactory(IEventMessageProcessor, IEventProcessingExceptionHandler, CancellationToken) instead. This constructor will be removed in version 1.0.0.")]
        public EventProcessorFactory(
            IMessageHandler messageHandler,
            IEventProcessingExceptionHandler exceptionHandler,
            CancellationToken cancellationToken)
        {
            _messageProcessor = new EventMessageProcessor(messageHandler);
            _exceptionHandler = exceptionHandler ?? throw new ArgumentNullException(nameof(exceptionHandler));
            _cancellationToken = cancellationToken;
        }
#pragma warning restore SA1642 // Constructor summary documentation must begin with standard text

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

            return new EventProcessor(_messageProcessor, _exceptionHandler, _cancellationToken);
        }
    }
}
