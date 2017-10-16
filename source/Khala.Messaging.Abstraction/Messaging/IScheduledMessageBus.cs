namespace Khala.Messaging
{
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Represents the client of the message bus that sends a message at the scheduled time.
    /// </summary>
    public interface IScheduledMessageBus
    {
        /// <summary>
        /// Sends an enveloped message to message bus at the scheduled time.
        /// </summary>
        /// <param name="envelope">A scheduled envelope message to be sent.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task Send(ScheduledEnvelope envelope, CancellationToken cancellationToken);
    }
}
