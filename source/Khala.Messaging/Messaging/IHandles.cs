namespace Khala.Messaging
{
    using System.Threading;
    using System.Threading.Tasks;

    public interface IHandles<TMessage>
        where TMessage : class
    {
        Task Handle(
            Envelope<TMessage> envelope,
            CancellationToken cancellationToken);
    }
}
