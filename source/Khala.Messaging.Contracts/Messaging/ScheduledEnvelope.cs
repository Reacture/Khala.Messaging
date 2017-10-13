namespace Khala.Messaging
{
    using System;

    public class ScheduledEnvelope
    {
        public ScheduledEnvelope(Envelope envelope, DateTimeOffset scheduledTime)
        {
            Envelope = envelope ?? throw new ArgumentNullException(nameof(envelope));
            ScheduledTime = scheduledTime;
        }

        public Envelope Envelope { get; }

        public DateTimeOffset ScheduledTime { get; }
    }
}
