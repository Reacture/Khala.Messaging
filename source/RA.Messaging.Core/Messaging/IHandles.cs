namespace ReactiveArchitecture.Messaging
{
    using System.Threading;
    using System.Threading.Tasks;

    public interface IHandles<TMessage>
        where TMessage : class
    {
        Task Handle(TMessage message, CancellationToken cancellationToken);
    }
}
