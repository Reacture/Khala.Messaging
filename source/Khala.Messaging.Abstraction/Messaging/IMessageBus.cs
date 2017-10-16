namespace Khala.Messaging
{
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Represents a message bus client.
    /// </summary>
    public interface IMessageBus
    {
        /// <summary>
        /// Sends a single enveloped message to message bus.
        /// </summary>
        /// <param name="envelope">An enveloped message to be sent.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task Send(
            Envelope envelope,
            CancellationToken cancellationToken);

        /// <summary>
        /// Sends multiple enveloped messages to message bus.
        /// </summary>
        /// <param name="envelopes">A seqeunce contains enveloped messages.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <remarks>If possible, the implementor sends messages sequentially and atomically.</remarks>
        Task Send(
            IEnumerable<Envelope> envelopes,
            CancellationToken cancellationToken);
    }
}
