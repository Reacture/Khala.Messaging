namespace ReactiveArchitecture.Messaging
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    public interface IHandles<TMessage>
        where TMessage : class
    {
        Task Handle(
            Guid messageId,
            Guid? correlationId,
            TMessage message,
            CancellationToken cancellationToken);
    }
}
