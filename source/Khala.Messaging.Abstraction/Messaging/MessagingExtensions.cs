namespace Khala.Messaging
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    public static class MessagingExtensions
    {
        public static Task Send(this IMessageBus messageBus, Envelope envelope)
        {
            if (messageBus == null)
            {
                throw new ArgumentNullException(nameof(messageBus));
            }

            if (envelope == null)
            {
                throw new ArgumentNullException(nameof(envelope));
            }

            return messageBus.Send(envelope, CancellationToken.None);
        }

        public static Task Send(
            this IMessageBus messageBus,
            IEnumerable<Envelope> envelopes)
        {
            if (messageBus == null)
            {
                throw new ArgumentNullException(nameof(messageBus));
            }

            if (envelopes == null)
            {
                throw new ArgumentNullException(nameof(envelopes));
            }

            return messageBus.Send(envelopes, CancellationToken.None);
        }

        public static Task Handle(
            this IMessageHandler messageHandler,
            Envelope envelope)
        {
            if (messageHandler == null)
            {
                throw new ArgumentNullException(nameof(envelope));
            }

            if (envelope == null)
            {
                throw new ArgumentNullException(nameof(envelope));
            }

            return messageHandler.Handle(envelope, CancellationToken.None);
        }

        public static Task Handle<TMessage>(
            this IHandles<TMessage> handles,
            Envelope<TMessage> envelope)
            where TMessage : class
        {
            if (handles == null)
            {
                throw new ArgumentNullException(nameof(handles));
            }

            if (envelope == null)
            {
                throw new ArgumentNullException(nameof(envelope));
            }

            return handles.Handle(envelope, CancellationToken.None);
        }
    }
}
