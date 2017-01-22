namespace ReactiveArchitecture.Messaging.Azure
{
    using System;
    using System.IO;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.ServiceBus.Messaging;

    public class ServiceBusQueueMessageProcessor
    {
        private static readonly IBrokeredMessageExceptionHandler _defaultExceptionHandler = new CompositeBrokeredMessageExceptionHandler();

        private readonly IMessageHandler _handler;
        private readonly IMessageSerializer _serializer;
        private readonly IBrokeredMessageExceptionHandler _exceptionHandler;
        private readonly CancellationToken _cancellationToken;

        private ServiceBusQueueMessageProcessor(
            IMessageHandler handler,
            IMessageSerializer serializer,
            IBrokeredMessageExceptionHandler exceptionHandler,
            CancellationToken cancellationToken)
        {
            _handler = handler;
            _serializer = serializer;
            _exceptionHandler = exceptionHandler;
            _cancellationToken = cancellationToken;
        }

        public static void Process(
            string connectionString,
            string queueName,
            IMessageHandler handler,
            IMessageSerializer serializer,
            IBrokeredMessageExceptionHandler exceptionHandler,
            CancellationToken cancellationToken)
        {
            if (connectionString == null)
            {
                throw new ArgumentNullException(nameof(connectionString));
            }

            if (queueName == null)
            {
                throw new ArgumentNullException(nameof(queueName));
            }

            if (handler == null)
            {
                throw new ArgumentNullException(nameof(handler));
            }

            if (serializer == null)
            {
                throw new ArgumentNullException(nameof(serializer));
            }

            if (exceptionHandler == null)
            {
                throw new ArgumentNullException(nameof(exceptionHandler));
            }

            var queueClient = QueueClient.CreateFromConnectionString(connectionString, queueName);

            var processor = new ServiceBusQueueMessageProcessor(
                handler,
                serializer,
                exceptionHandler,
                cancellationToken);

            queueClient.OnMessageAsync(processor.ProcessMessage);

            cancellationToken.Register(queueClient.Close);
        }

        public static void Process(
            string connectionString,
            string queueName,
            IMessageHandler handler,
            IMessageSerializer serializer,
            CancellationToken cancellationToken)
        {
            Process(
                connectionString,
                queueName,
                handler,
                serializer,
                _defaultExceptionHandler,
                cancellationToken);
        }

        internal async Task ProcessMessage(BrokeredMessage brokeredMessage)
        {
            using (var stream = brokeredMessage.GetBody<Stream>())
            using (var memory = new MemoryStream())
            {
                await stream.CopyToAsync(memory, 81920, _cancellationToken).ConfigureAwait(false);
                byte[] bytes = memory.ToArray();

                try
                {
                    string value = Encoding.UTF8.GetString(bytes);
                    object message = _serializer.Deserialize(value);

                    try
                    {
                        await _handler.Handle(message, _cancellationToken).ConfigureAwait(false);
                    }
                    catch (Exception exception)
                    {
                        var exceptionContext = new HandleMessageExceptionContext(message, exception);
                        _exceptionHandler.HandleMessageException(exceptionContext);
                        if (exceptionContext.Handled == false)
                        {
                            throw;
                        }
                    }
                }
                catch (Exception exception)
                {
                    var exceptionContext = new HandleBrokeredMessageExceptionContext(brokeredMessage, bytes, exception);
                    _exceptionHandler.HandleBrokeredMessageException(exceptionContext);
                    if (exceptionContext.Handled == false)
                    {
                        throw;
                    }
                }
            }

            await brokeredMessage.CompleteAsync();
        }
    }
}
