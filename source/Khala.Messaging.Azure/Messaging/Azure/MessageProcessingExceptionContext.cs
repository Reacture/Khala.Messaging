namespace Khala.Messaging.Azure
{
    using System;

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
            Envelope envelope,
            Exception exception)
            : this(source, exception)
        {
            if (envelope == null)
            {
                throw new ArgumentNullException(nameof(envelope));
            }

            Envelope = envelope;
        }

        public TSource Source { get; }

        public Envelope Envelope { get; }

        public Exception Exception { get; }

        public bool Handled { get; set; } = false;
    }
}
