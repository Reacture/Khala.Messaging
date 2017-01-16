namespace ReactiveArchitecture.Messaging
{
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    public interface IMessageBus
    {
        Task Send(
            object message,
            CancellationToken cancellationToken);

        Task SendBatch(
            IEnumerable<object> messages,
            CancellationToken cancellationToken);
    }
}
