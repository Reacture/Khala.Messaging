namespace Khala.Messaging.Azure
{
    using System;

    public sealed class MessageProcessingExceptionContext<TSource>
        where TSource : class
    {
        public MessageProcessingExceptionContext(
            TSource source,
            Exception exception)
        {
            Source = source ?? throw new ArgumentNullException(nameof(source));
            Exception = exception ?? throw new ArgumentNullException(nameof(exception));
        }

        public MessageProcessingExceptionContext(
            TSource source,
            Envelope envelope,
            Exception exception)
            : this(source, exception)
        {
            Envelope = envelope ?? throw new ArgumentNullException(nameof(envelope));
        }

        public TSource Source { get; }

        public Envelope Envelope { get; }

        public Exception Exception { get; }

        public bool Handled { get; set; } = false;
    }
}
