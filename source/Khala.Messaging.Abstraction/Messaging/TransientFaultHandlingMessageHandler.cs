namespace Khala.Messaging
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Khala.TransientFaultHandling;

    /// <summary>
    /// Provides a retry policy applied decorator of an <see cref="IMessageHandler"/> instance.
    /// </summary>
    public class TransientFaultHandlingMessageHandler : IMessageHandler
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TransientFaultHandlingMessageHandler"/> class.
        /// </summary>
        /// <param name="retryPolicy">An <see cref="IRetryPolicy"/> object.</param>
        /// <param name="messageHandler">As <see cref="IMessageHandler"/> object.</param>
        public TransientFaultHandlingMessageHandler(IRetryPolicy retryPolicy, IMessageHandler messageHandler)
        {
            RetryPolicy = retryPolicy ?? throw new ArgumentNullException(nameof(retryPolicy));
            MessageHandler = messageHandler ?? throw new ArgumentNullException(nameof(messageHandler));
        }

        /// <summary>
        /// Gets the retry policy to be applied to handle messages.
        /// </summary>
        /// <value>
        /// The retry policy to be applied to handle messages.
        /// </value>
        public IRetryPolicy RetryPolicy { get; }

        /// <summary>
        /// Gets the message handler.
        /// </summary>
        /// <value>
        /// The message handler.
        /// </value>
        public IMessageHandler MessageHandler { get; }

        /// <summary>
        /// Handles a message appling <see cref="RetryPolicy"/>.
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

            return RetryPolicy.Run(MessageHandler.Handle, envelope, cancellationToken);
        }
    }
}
