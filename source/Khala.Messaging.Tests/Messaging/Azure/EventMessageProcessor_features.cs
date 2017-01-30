using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.ServiceBus.Messaging;
using Moq;
using Newtonsoft.Json;
using Xunit;

namespace Khala.Messaging.Azure
{
    public class EventMessageProcessor_features
    {
        private IMessageHandler messageHandler;
        private JsonMessageSerializer messageSerializer;
        private IMessageProcessingExceptionHandler<EventData> exceptionHandler;
        private EventMessageProcessorFactory factory;

        public EventMessageProcessor_features()
        {
            messageHandler = Mock.Of<IMessageHandler>();
            messageSerializer = new JsonMessageSerializer();
            exceptionHandler = Mock.Of<IMessageProcessingExceptionHandler<EventData>>();
            factory = new EventMessageProcessorFactory(
                messageHandler,
                messageSerializer,
                exceptionHandler,
                CancellationToken.None);
        }

        [Fact]
        public void ProcessEventsAsync_invokes_exception_handler_if_GetBytes_fails()
        {
            var partitionContext = new PartitionContext();
            var eventData = new EventData();
            eventData.Dispose();
            var sut = (EventMessageProcessor)factory.CreateEventProcessor(partitionContext);

            Func<Task> action = () =>
            sut.ProcessEventsAsync(partitionContext, new[] { eventData });

            action.ShouldThrow<ObjectDisposedException>();
            Mock.Get(exceptionHandler).Verify(
                x =>
                x.Handle(
                    It.Is<MessageProcessingExceptionContext<EventData>>(
                        p =>
                        p.Source == eventData &&
                        p.Body == null &&
                        p.Envelope == null &&
                        p.Exception is ObjectDisposedException &&
                        p.Handled == false)),
                Times.Once());
        }

        [Fact]
        public void ProcessEventsAsync_invokes_exception_handler_if_serialization_fails()
        {
            var partitionContext = new PartitionContext();
            var body = new byte[] { 1, 2, 3 };
            var eventData = new EventData(body);
            var sut = (EventMessageProcessor)factory.CreateEventProcessor(partitionContext);

            Func<Task> action = () =>
            sut.ProcessEventsAsync(partitionContext, new[] { eventData });

            action.ShouldThrow<JsonReaderException>();
            Mock.Get(exceptionHandler).Verify(
                x =>
                x.Handle(
                    It.Is<MessageProcessingExceptionContext<EventData>>(
                        p =>
                        p.Source == eventData &&
                        p.Body != null &&
                        p.Body.SequenceEqual(body) &&
                        p.Envelope == null &&
                        p.Exception is JsonReaderException &&
                        p.Handled == false)),
                Times.Once());
        }

        [Fact]
        public void ProcessEventsAsync_invokes_exception_handler_if_message_handling_fails()
        {
            // Arrange
            var partitionContext = new PartitionContext();

            var correlationId = Guid.NewGuid();
            var envelope = new Envelope(correlationId, "foo");
            string value = messageSerializer.Serialize(envelope);
            byte[] body = Encoding.UTF8.GetBytes(value);
            var eventData = new EventData(body);

            Mock.Get(messageHandler)
                .Setup(x => x.Handle(It.IsAny<Envelope>(), CancellationToken.None))
                .Throws<InvalidOperationException>();

            var sut = (EventMessageProcessor)factory.CreateEventProcessor(partitionContext);

            // Act
            Func<Task> action = () =>
            sut.ProcessEventsAsync(partitionContext, new[] { eventData });

            // Assert
            action.ShouldThrow<InvalidOperationException>();
            Mock.Get(exceptionHandler).Verify(
                x =>
                x.Handle(
                    It.Is<MessageProcessingExceptionContext<EventData>>(
                        p =>
                        p.Source == eventData &&
                        p.Body != null &&
                        p.Body.SequenceEqual(body) &&
                        p.Envelope != null &&
                        p.Envelope.MessageId == envelope.MessageId &&
                        p.Envelope.CorrelationId == correlationId &&
                        p.Envelope.Message.Equals(envelope.Message) &&
                        p.Exception is InvalidOperationException &&
                        p.Handled == false)),
                Times.Once());
        }

        [Fact]
        public void ProcessEventsAsync_does_not_throw_if_exception_handler_handles_exception()
        {
            var partitionContext = new PartitionContext();
            var eventData = new EventData();
            eventData.Dispose();
            Mock.Get(exceptionHandler)
                .Setup(
                    x =>
                    x.Handle(
                        It.IsAny<MessageProcessingExceptionContext<EventData>>()))
                .Callback<MessageProcessingExceptionContext<EventData>>(p => p.Handled = true)
                .Returns(Task.FromResult(true));
            var sut = (EventMessageProcessor)factory.CreateEventProcessor(partitionContext);

            Func<Task> action = () =>
            sut.ProcessEventsAsync(partitionContext, new[] { eventData });

            action.ShouldNotThrow();
        }

        [Fact]
        public void ProcessEventsAsync_ignores_exception_handler_error()
        {
            var partitionContext = new PartitionContext();
            var eventData = new EventData();
            eventData.Dispose();
            Mock.Get(exceptionHandler)
                .Setup(
                    x =>
                    x.Handle(
                        It.IsAny<MessageProcessingExceptionContext<EventData>>()))
                .Callback<MessageProcessingExceptionContext<EventData>>(p => p.Handled = true)
                .Throws<InvalidOperationException>();
            var sut = (EventMessageProcessor)factory.CreateEventProcessor(partitionContext);

            Func<Task> action = () =>
            sut.ProcessEventsAsync(partitionContext, new[] { eventData });

            action.ShouldNotThrow();
        }
    }
}
