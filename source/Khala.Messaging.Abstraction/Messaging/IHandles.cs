namespace Khala.Messaging
{
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Makes the <see cref="InterfaceAwareHandler"/> derived class to handle messages of type <typeparamref name="TMessage"/>.
    /// </summary>
    /// <typeparam name="TMessage">The type of the message.</typeparam>
    public interface IHandles<TMessage>
        where TMessage : class
    {
        /// <summary>
        /// Handles a strong-typed message.
        /// </summary>
        /// <param name="envelope">An <see cref="Envelope{TMessage}"/> that contains the message object and related properties.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task Handle(
            Envelope<TMessage> envelope,
            CancellationToken cancellationToken);
    }
}
