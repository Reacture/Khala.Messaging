namespace Arcane.Messaging.Azure
{
    using System;
    using System.Collections.Generic;

    public class MessageProcessingExceptionContext<TSource>
        where TSource : class
    {
        public MessageProcessingExceptionContext(
            TSource source,
            Exception exception)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (exception == null)
            {
                throw new ArgumentNullException(nameof(exception));
            }

            Source = source;
            Exception = exception;
        }

        public MessageProcessingExceptionContext(
            TSource source,
            IReadOnlyList<byte> body,
            Exception exception)
            : this(source, exception)
        {
            if (body == null)
            {
                throw new ArgumentNullException(nameof(body));
            }

            Body = body;
        }

        public MessageProcessingExceptionContext(
            TSource source,
            IReadOnlyList<byte> body,
            Envelope envelope,
            Exception exception)
            : this(source, body, exception)
        {
            if (envelope == null)
            {
                throw new ArgumentNullException(nameof(envelope));
            }

            Envelope = envelope;
        }

        public TSource Source { get; }

        public IReadOnlyList<byte> Body { get; }

        public Envelope Envelope { get; }

        public Exception Exception { get; }

        public bool Handled { get; set; } = false;
    }
}
