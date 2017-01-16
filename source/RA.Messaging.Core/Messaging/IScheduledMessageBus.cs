namespace ReactiveArchitecture.Messaging
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    public interface IScheduledMessageBus
    {
        Task Send(
            object message,
            DateTimeOffset scheduledTimeUtc,
            CancellationToken cancellationToken);
    }
}
