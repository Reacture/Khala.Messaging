namespace ReactiveArchitecture.Messaging.Azure
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;

    public class CompositeEventMessageExceptionHandler : IEventMessageExceptionHandler
    {
        private readonly IEnumerable<IEventMessageExceptionHandler> _exceptionHandlers;

        public CompositeEventMessageExceptionHandler(
            params IEventMessageExceptionHandler[] exceptionHandlers)
        {
            if (exceptionHandlers == null)
            {
                throw new ArgumentNullException(nameof(exceptionHandlers));
            }

            var handlerList = new List<IEventMessageExceptionHandler>(exceptionHandlers);

            for (int i = 0; i < handlerList.Count; i++)
            {
                if (handlerList[i] == null)
                {
                    throw new ArgumentException(
                        $"{nameof(exceptionHandlers)} cannot contain null exception handler.",
                        nameof(exceptionHandlers));
                }
            }

            _exceptionHandlers = new ReadOnlyCollection<IEventMessageExceptionHandler>(handlerList);
        }

        public void HandleEventException(HandleEventExceptionContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            foreach (IEventMessageExceptionHandler handler in _exceptionHandlers)
            {
                handler.HandleEventException(context);
            }
        }

        public void HandleMessageException(HandleMessageExceptionContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            foreach (IEventMessageExceptionHandler handler in _exceptionHandlers)
            {
                handler.HandleMessageException(context);
            }
        }
    }
}
