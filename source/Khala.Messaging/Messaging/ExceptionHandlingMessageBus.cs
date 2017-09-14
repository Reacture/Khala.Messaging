namespace Khala.Messaging
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    public class ExceptionHandlingMessageBus : IMessageBus
    {
        public ExceptionHandlingMessageBus(IMessageBus messageBus, IMessageBusExceptionHandler exceptionHandler)
        {
            MessageBus = messageBus ?? throw new ArgumentNullException(nameof(messageBus));
            ExceptionHandler = exceptionHandler ?? throw new ArgumentNullException(nameof(exceptionHandler));
        }

        public IMessageBus MessageBus { get; }

        public IMessageBusExceptionHandler ExceptionHandler { get; }

        public Task Send(Envelope envelope, CancellationToken cancellationToken)
        {
            if (envelope == null)
            {
                throw new ArgumentNullException(nameof(envelope));
            }

            async Task Run()
            {
                try
                {
                    await MessageBus.Send(envelope, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception exception)
                {
                    var context = new MessageBusExceptionContext(new[] { envelope }, exception);
                    await ExceptionHandler.Handle(context).ConfigureAwait(false);
                    if (context.Handled == false)
                    {
                        throw;
                    }
                }
            }

            return Run();
        }

        public Task SendBatch(IEnumerable<Envelope> envelopes, CancellationToken cancellationToken)
        {
            if (envelopes == null)
            {
                throw new ArgumentNullException(nameof(envelopes));
            }

            List<Envelope> envelopeList = envelopes.ToList();

            async Task Run()
            {
                try
                {
                    await MessageBus.SendBatch(envelopeList, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception exception)
                {
                    var context = new MessageBusExceptionContext(envelopeList, exception);
                    await ExceptionHandler.Handle(context).ConfigureAwait(false);
                    if (context.Handled == false)
                    {
                        throw;
                    }
                }
            }

            return Run();
        }
    }
}
