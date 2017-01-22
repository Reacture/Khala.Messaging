namespace ReactiveArchitecture.Messaging.Azure
{
    using System;
    using System.Collections.Generic;
    using Microsoft.ServiceBus.Messaging;

    public class HandleBrokeredMessageExceptionContext
    {
        public HandleBrokeredMessageExceptionContext(
            BrokeredMessage message,
            IReadOnlyList<byte> body,
            Exception exception)
        {
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            if (body == null)
            {
                throw new ArgumentNullException(nameof(body));
            }

            if (exception == null)
            {
                throw new ArgumentNullException(nameof(exception));
            }

            Message = message;
            Body = body;
            Exception = exception;
        }

        public BrokeredMessage Message { get; }

        public IReadOnlyList<byte> Body { get; }

        public Exception Exception { get; }

        public bool Handled { get; set; } = false;
    }
}
