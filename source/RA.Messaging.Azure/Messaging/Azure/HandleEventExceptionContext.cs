namespace ReactiveArchitecture.Messaging.Azure
{
    using System;
    using System.Collections.Generic;
    using Microsoft.ServiceBus.Messaging;

    public class HandleEventExceptionContext
    {
        public HandleEventExceptionContext(
            EventData eventData, IReadOnlyList<byte> body, Exception exception)
        {
            if (eventData == null)
            {
                throw new ArgumentNullException(nameof(eventData));
            }

            if (body == null)
            {
                throw new ArgumentNullException(nameof(body));
            }

            if (exception == null)
            {
                throw new ArgumentNullException(nameof(exception));
            }

            EventData = eventData;
            Body = body;
            Exception = exception;
        }

        public EventData EventData { get; }

        public IReadOnlyList<byte> Body { get; }

        public Exception Exception { get; }

        public bool Handled { get; set; } = false;
    }
}
