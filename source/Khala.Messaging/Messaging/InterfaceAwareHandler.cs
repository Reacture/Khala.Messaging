namespace Khala.Messaging
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Reflection;
    using System.Threading;
    using System.Threading.Tasks;

    public abstract class InterfaceAwareHandler : IMessageHandler
    {
        private readonly IReadOnlyDictionary<Type, Handler> _handlers;

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
                MethodInfo factory =
                    factoryTemplate.MakeGenericMethod(typeArguments);
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

        public Task Handle(
            Envelope envelope, CancellationToken cancellationToken)
        {
            if (envelope == null)
            {
                throw new ArgumentNullException(nameof(envelope));
            }

            return HandleMessage(envelope, cancellationToken);
        }

        private async Task HandleMessage(
            Envelope envelope, CancellationToken cancellationToken)
        {
            Handler handler;
            if (_handlers.TryGetValue(envelope.Message.GetType(), out handler))
            {
                await handler.Invoke(envelope, cancellationToken);
            }
        }
    }
}
