namespace ReactiveArchitecture.Messaging
{
    using System.Threading;
    using System.Threading.Tasks;

    public interface IMessageHandler
    {
        Task Handle(object message, CancellationToken cancellationToken);
    }
}
