namespace ReactiveArchitecture.Messaging.Azure
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;

    public class CompositeBrokeredMessageExceptionHandler : IBrokeredMessageExceptionHandler
    {
        private readonly IEnumerable<IBrokeredMessageExceptionHandler> _exceptionHandlers;

        public CompositeBrokeredMessageExceptionHandler(
            params IBrokeredMessageExceptionHandler[] exceptionHandlers)
        {
            if (exceptionHandlers == null)
            {
                throw new ArgumentNullException(nameof(exceptionHandlers));
            }

            var handlerList = new List<IBrokeredMessageExceptionHandler>(exceptionHandlers);

            for (int i = 0; i < handlerList.Count; i++)
            {
                if (handlerList[i] == null)
                {
                    throw new ArgumentException(
                        $"{nameof(exceptionHandlers)} cannot contain null exception handler.",
                        nameof(exceptionHandlers));
                }
            }

            _exceptionHandlers = new ReadOnlyCollection<IBrokeredMessageExceptionHandler>(handlerList);
        }

        public void HandleBrokeredMessageException(HandleBrokeredMessageExceptionContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            foreach (IBrokeredMessageExceptionHandler handler in _exceptionHandlers)
            {
                handler.HandleBrokeredMessageException(context);
            }
        }

        public void HandleMessageException(HandleMessageExceptionContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            foreach (IBrokeredMessageExceptionHandler handler in _exceptionHandlers)
            {
                handler.HandleMessageException(context);
            }
        }
    }
}
