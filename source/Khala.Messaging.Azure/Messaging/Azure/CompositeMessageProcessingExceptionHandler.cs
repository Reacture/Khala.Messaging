namespace Khala.Messaging.Azure
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Threading.Tasks;

    public sealed class CompositeMessageProcessingExceptionHandler<TSource> :
        IMessageProcessingExceptionHandler<TSource>
        where TSource : class
    {
        private IEnumerable<IMessageProcessingExceptionHandler<TSource>> _handlers;

        public CompositeMessageProcessingExceptionHandler(
            params IMessageProcessingExceptionHandler<TSource>[] handlers)
        {
            if (handlers == null)
            {
                throw new ArgumentNullException(nameof(handlers));
            }

            var handlerList = new List<IMessageProcessingExceptionHandler<TSource>>(handlers);

            for (int i = 0; i < handlerList.Count; i++)
            {
                if (handlerList[i] == null)
                {
                    throw new ArgumentException(
                        $"{nameof(handlers)} cannot contain null.",
                        nameof(handlers));
                }
            }

            _handlers = handlerList;
        }

        public Task Handle(MessageProcessingExceptionContext<TSource> context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            return HandleException(context);
        }

        private async Task HandleException(MessageProcessingExceptionContext<TSource> context)
        {
            foreach (IMessageProcessingExceptionHandler<TSource> handler in _handlers)
            {
                try
                {
                    await handler.Handle(context).ConfigureAwait(false);
                }
                catch (Exception handlerError)
                {
                    Trace.TraceError(handlerError.ToString());
                }
            }
        }
    }
}
