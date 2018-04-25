namespace Khala.Messaging.Azure
{
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.EventHubs;

    /// <summary>
    /// Represents an event message processor that processes message from EventHub.
    /// </summary>
    public interface IEventMessageProcessor
    {
        /// <summary>
        /// Process a message.
        /// </summary>
        /// <param name="envelope">An <see cref="Envelope"/> that contains the message object and related properties.</param>
        /// <param name="properties">A property bag from <see cref="EventData"/>.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task Process(
            Envelope envelope,
            IDictionary<string, object> properties,
            CancellationToken cancellationToken);
    }
}
