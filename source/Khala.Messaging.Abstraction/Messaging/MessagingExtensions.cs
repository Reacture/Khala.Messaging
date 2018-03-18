namespace Khala.Messaging
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Extension methods that support messaging interfaces.
    /// </summary>
    public static class MessagingExtensions
    {
        /// <summary>
        /// Sends a single enveloped message to message bus.
        /// </summary>
        /// <param name="messageBus">A message bus client.</param>
        /// <param name="envelope">An enveloped message to be sent.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
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

        /// <summary>
        /// Sends multiple enveloped messages to message bus.
        /// </summary>
        /// <param name="messageBus">A message bus client.</param>
        /// <param name="envelopes">A seqeunce contains enveloped messages.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
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

        /// <summary>
        /// Handles a message.
        /// </summary>
        /// <param name="messageHandler">A message handler.</param>
        /// <param name="envelope">An <see cref="Envelope"/> that contains the message object and related properties.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public static Task Handle(
            this IMessageHandler messageHandler,
            Envelope envelope)
        {
            if (messageHandler == null)
            {
                throw new ArgumentNullException(nameof(messageHandler));
            }

            if (envelope == null)
            {
                throw new ArgumentNullException(nameof(envelope));
            }

            return messageHandler.Handle(envelope, CancellationToken.None);
        }

        /// <summary>
        /// Handles a strong-typed message.
        /// </summary>
        /// <typeparam name="TMessage">The type of the message.</typeparam>
        /// <param name="handles">A message handler that handles messages of <typeparamref name="TMessage"/>.</param>
        /// <param name="envelope">An <see cref="Envelope{TMessage}"/> that contains the message object and related properties.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
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
