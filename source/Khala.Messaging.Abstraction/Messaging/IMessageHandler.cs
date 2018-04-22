namespace Khala.Messaging
{
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Represents a message handler.
    /// </summary>
    public interface IMessageHandler
    {
        /// <summary>
        /// Determines whether to accept the specified message.
        /// </summary>
        /// <param name="envelope">An <see cref="Envelope"/> that contains the message object and related properties.</param>
        /// <returns><c>true</c> if the message handler accepts the message; otherwise, <c>false</c>.</returns>
        bool Accepts(Envelope envelope);

        /// <summary>
        /// Handles a message.
        /// </summary>
        /// <param name="envelope">An <see cref="Envelope"/> that contains the message object and related properties.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task Handle(Envelope envelope, CancellationToken cancellationToken);
    }
}
