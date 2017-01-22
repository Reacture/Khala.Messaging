namespace ReactiveArchitecture.Messaging.Azure
{
    using System;

    public class HandleMessageExceptionContext
    {
        public HandleMessageExceptionContext(
            object message, Exception exception)
        {
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            if (exception == null)
            {
                throw new ArgumentNullException(nameof(exception));
            }

            Message = message;
            Exception = exception;
        }

        public object Message { get; }

        public Exception Exception { get; }

        public bool Handled { get; set; } = false;
    }
}
