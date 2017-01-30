namespace Khala.Messaging
{
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    public interface IMessageBus
    {
        Task Send(
            Envelope envelope,
            CancellationToken cancellationToken);

        Task SendBatch(
            IEnumerable<Envelope> envelopes,
            CancellationToken cancellationToken);
    }
}
