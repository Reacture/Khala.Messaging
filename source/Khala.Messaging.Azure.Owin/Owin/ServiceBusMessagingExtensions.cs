namespace Owin
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Khala.Messaging.Azure;
    using Microsoft.Azure.ServiceBus;
    using Microsoft.Azure.ServiceBus.Core;
    using Microsoft.Owin.BuilderProperties;

    public static class ServiceBusMessagingExtensions
    {
        public static void UseServiceBusMessageProcessor(
            this IAppBuilder app,
            IReceiverClient receiverClient,
            MessageProcessor<Message> messageProcessor)
        {
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }

            if (receiverClient == null)
            {
                throw new ArgumentNullException(nameof(receiverClient));
            }

            if (messageProcessor == null)
            {
                throw new ArgumentNullException(nameof(messageProcessor));
            }

            CancellationToken cancellationToken = new AppProperties(app.Properties).OnAppDisposing;
            Start(receiverClient, messageProcessor, cancellationToken);
            cancellationToken.Register(() => Stop(receiverClient));
        }

        private static void Start(
            IReceiverClient receiverClient,
            MessageProcessor<Message> messageProcessor,
            CancellationToken cancellationToken)
        {
            var processor = new ServiceBusMessageProcessor(receiverClient, messageProcessor, cancellationToken);
            var handlerOptions = new MessageHandlerOptions(ExceptionReceivedHandler) { AutoComplete = false };
            receiverClient.RegisterMessageHandler(processor.ProcessMessage, handlerOptions);
        }

        private static Task ExceptionReceivedHandler(ExceptionReceivedEventArgs exceptionReceivedEventArgs) => Task.CompletedTask;

        private static void Stop(IReceiverClient receiverClient) => receiverClient.CloseAsync().Wait();
    }
}
