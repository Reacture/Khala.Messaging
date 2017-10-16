namespace Khala.Messaging
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Reflection;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Provides an <see cref="IMessageHandler"/> implementation that routes messages to strong-typed message handlers defined by <see cref="IHandles{TMessage}"/> interfaces.
    /// </summary>
    /// <example>
    /// This sample shows how to use the <see cref="InterfaceAwareHandler"/> class.
    /// <code>
    /// <![CDATA[
    /// public class OrderEventHandler :
    ///     InterfaceAwareHandler,
    ///     IHandles<OrderPlaced>,
    ///     IHandles<OrderCanceled>
    /// {
    ///     public async Task Handle(Envelope<OrderPlaced> envelope, CancellationToken cancellationToken)
    ///     {
    ///         // Envelopes that contain OrderPlaced messages are routed to this method.
    ///     }
    ///
    ///     public async Task Handle(Envelope<OrderCanceled> envelope, CancellationToken cancellationToken)
    ///     {
    ///         // Envelopes that contain OrderCanceled messages are routed to this method.
    ///     }
    /// }
    /// ]]>
    /// </code>
    /// </example>
    public abstract class InterfaceAwareHandler : IMessageHandler
    {
        private readonly IReadOnlyDictionary<Type, Handler> _handlers;

        /// <summary>
        /// Initializes a new instance of the <see cref="InterfaceAwareHandler"/> class.
        /// </summary>
        protected InterfaceAwareHandler()
        {
            var handlers = new Dictionary<Type, Handler>();

            WireupHandlers(handlers);

            _handlers = new ReadOnlyDictionary<Type, Handler>(handlers);
        }

        private delegate Task Handler(
            Envelope envelope,
            CancellationToken cancellationToken);

        private void WireupHandlers(Dictionary<Type, Handler> handlers)
        {
            MethodInfo factoryTemplate = typeof(InterfaceAwareHandler)
                .GetTypeInfo()
                .GetDeclaredMethod(nameof(GetMessageHandler));

            IEnumerable<Type> query =
                from t in GetType().GetTypeInfo().ImplementedInterfaces
                where
                    t.IsConstructedGenericType &&
                    t.GetGenericTypeDefinition() == typeof(IHandles<>)
                select t;

            foreach (Type t in query)
            {
                Type[] typeArguments = t.GenericTypeArguments;
                MethodInfo factory = factoryTemplate.MakeGenericMethod(typeArguments);
                var handler = (Handler)factory.Invoke(this, null);
                handlers[typeArguments[0]] = handler;
            }
        }

        private Handler GetMessageHandler<TMessage>()
            where TMessage : class
        {
            var handler = (IHandles<TMessage>)this;
            return (envelope, cancellationToken) =>
            {
                return handler.Handle(
                    new Envelope<TMessage>(
                        envelope.MessageId,
                        envelope.CorrelationId,
                        (TMessage)envelope.Message),
                    cancellationToken);
            };
        }

        /// <summary>
        /// Handles a message with a strong-typed message handler.
        /// </summary>
        /// <param name="envelope">An <see cref="Envelope"/> that contains the message object and related properties.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public Task Handle(
            Envelope envelope, CancellationToken cancellationToken)
        {
            if (envelope == null)
            {
                throw new ArgumentNullException(nameof(envelope));
            }

            return _handlers.TryGetValue(envelope.Message.GetType(), out Handler handler)
                ? handler.Invoke(envelope, cancellationToken)
                : Task.CompletedTask;
        }
    }
}
