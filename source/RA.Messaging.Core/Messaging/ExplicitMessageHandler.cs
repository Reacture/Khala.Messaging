namespace ReactiveArchitecture.Messaging
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Reflection;
    using System.Threading;
    using System.Threading.Tasks;

    public abstract class ExplicitMessageHandler : IMessageHandler
    {
        private readonly IReadOnlyDictionary<Type, Handler> _handlers;

        protected ExplicitMessageHandler()
        {
            MethodInfo factoryTemplate = typeof(ExplicitMessageHandler)
                .GetTypeInfo()
                .GetDeclaredMethod(nameof(GetHandler));

            var handlers = new Dictionary<Type, Handler>();

            var query = from t in GetType().GetTypeInfo().ImplementedInterfaces
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

            _handlers = new ReadOnlyDictionary<Type, Handler>(handlers);
        }

        private delegate Task Handler(
            object message,
            CancellationToken cancellationToken);

        async Task IMessageHandler.Handle(
            object message,
            CancellationToken cancellationToken)
        {
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            Handler handler;
            if (_handlers.TryGetValue(message.GetType(), out handler))
            {
                await handler.Invoke(message, cancellationToken);
            }
        }

        private Handler GetHandler<TMessage>()
            where TMessage : class
        {
            var handler = (IHandles<TMessage>)this;
            return (message, cancellationToken) =>
                handler.Handle((TMessage)message, cancellationToken);
        }
    }
}
