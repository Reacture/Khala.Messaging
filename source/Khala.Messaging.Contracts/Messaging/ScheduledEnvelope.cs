namespace Khala.Messaging
{
    using System;

    /// <summary>
    /// Encapsulates <see cref="Messaging.Envelope"/> object and the time at which the message will be sent.
    /// </summary>
    public sealed class ScheduledEnvelope
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ScheduledEnvelope"/> class.
        /// </summary>
        /// <param name="envelope">An envelope object.</param>
        /// <param name="scheduledTime">The time at which <paramref name="scheduledTime"/> will be sent.</param>
        public ScheduledEnvelope(Envelope envelope, DateTimeOffset scheduledTime)
        {
            Envelope = envelope ?? throw new ArgumentNullException(nameof(envelope));
            ScheduledTime = scheduledTime;
        }

        /// <summary>
        /// Gets the envelope object.
        /// </summary>
        /// <value>
        /// The envelope object.
        /// </value>
        public Envelope Envelope { get; }

        /// <summary>
        /// Gets the time at which <see cref="Envelope"/> will be sent.
        /// </summary>
        /// <value>
        /// The time at which <see cref="Envelope"/> will be sent.
        /// </value>
        public DateTimeOffset ScheduledTime { get; }
    }
}
