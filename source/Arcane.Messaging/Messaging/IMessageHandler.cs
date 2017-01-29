namespace Arcane.Messaging
{
    using System.Threading;
    using System.Threading.Tasks;

    public interface IMessageHandler
    {
        Task Handle(Envelope envelope, CancellationToken cancellationToken);
    }
}
