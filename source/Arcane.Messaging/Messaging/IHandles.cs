namespace Arcane.Messaging
{
    using System.Threading;
    using System.Threading.Tasks;

    public interface IHandles<TMessage>
        where TMessage : class
    {
        Task Handle(
            ReceivedEnvelope<TMessage> envelope,
            CancellationToken cancellationToken);
    }
}
