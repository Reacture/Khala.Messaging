namespace Khala.Messaging
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Khala.TransientFaultHandling;

    /// <summary>
    /// Provides a retry policy applied decorator of an <see cref="IMessageBus"/> instance.
    /// </summary>
    public class TransientFaultHandlingMessageBus : IMessageBus
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TransientFaultHandlingMessageBus"/> class.
        /// </summary>
        /// <param name="retryPolicy">An <see cref="IRetryPolicy"/> object.</param>
        /// <param name="messageBus">An <see cref="IMessageBus"/> object.</param>
        public TransientFaultHandlingMessageBus(IRetryPolicy retryPolicy, IMessageBus messageBus)
        {
            RetryPolicy = retryPolicy ?? throw new ArgumentNullException(nameof(retryPolicy));
            MessageBus = messageBus ?? throw new ArgumentNullException(nameof(messageBus));
        }

        /// <summary>
        /// Gets the retry policy to be applied to send messages.
        /// </summary>
        /// <value>
        /// The retry policy to be applied to send messages.
        /// </value>
        public IRetryPolicy RetryPolicy { get; }

        /// <summary>
        /// Gets the message bus client.
        /// </summary>
        /// <value>
        /// The message bus client.
        /// </value>
        public IMessageBus MessageBus { get; }

        /// <summary>
        /// Sends a single enveloped message to message bus appling <see cref="RetryPolicy"/>.
        /// </summary>
        /// <param name="envelope">An enveloped message to be sent.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public Task Send(Envelope envelope, CancellationToken cancellationToken)
        {
            if (envelope == null)
            {
                throw new ArgumentNullException(nameof(envelope));
            }

            return RetryPolicy.Run(MessageBus.Send, envelope, cancellationToken);
        }

        /// <summary>
        /// Sends multiple enveloped messages to message bus appling <see cref="RetryPolicy"/>.
        /// </summary>
        /// <param name="envelopes">A seqeunce contains enveloped messages.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public Task Send(IEnumerable<Envelope> envelopes, CancellationToken cancellationToken)
        {
            if (envelopes == null)
            {
                throw new ArgumentNullException(nameof(envelopes));
            }

            return RetryPolicy.Run(MessageBus.Send, envelopes, cancellationToken);
        }
    }
}
