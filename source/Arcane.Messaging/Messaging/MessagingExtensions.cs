namespace Arcane.Messaging
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

            return messageBus.Send(envelope, CancellationToken.None);
        }

        public static Task SendBatch(
            this IMessageBus messageBus,
            IEnumerable<Envelope> envelopes)
        {
            if (messageBus == null)
            {
                throw new ArgumentNullException(nameof(messageBus));
            }

            return messageBus.SendBatch(envelopes, CancellationToken.None);
        }
    }
}
