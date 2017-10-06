namespace Khala.Messaging.Azure
{
    using System;

    public sealed class MessageProcessingExceptionContext<TData>
        where TData : class
    {
        public MessageProcessingExceptionContext(TData data, Exception exception)
        {
            Data = data ?? throw new ArgumentNullException(nameof(data));
            Exception = exception ?? throw new ArgumentNullException(nameof(exception));
        }

        public MessageProcessingExceptionContext(
            TData data,
            Envelope envelope,
            Exception exception)
            : this(data, exception)
        {
            Envelope = envelope ?? throw new ArgumentNullException(nameof(envelope));
        }

        public TData Data { get; }

        public Envelope Envelope { get; }

        public Exception Exception { get; }

        public bool Handled { get; set; } = false;
    }
}
