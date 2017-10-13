namespace Khala.Messaging
{
    using System.Threading;
    using System.Threading.Tasks;

    public interface IScheduledMessageBus
    {
        Task Send(ScheduledEnvelope envelope, CancellationToken cancellationToken);
    }
}
