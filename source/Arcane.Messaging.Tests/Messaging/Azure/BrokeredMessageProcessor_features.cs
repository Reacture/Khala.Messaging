using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.ServiceBus.Messaging;
using Moq;
using Newtonsoft.Json;
using Ploeh.AutoFixture;
using Ploeh.AutoFixture.AutoMoq;
using Ploeh.AutoFixture.Idioms;
using Xunit;

namespace Arcane.Messaging.Azure
{
    public class BrokeredMessageProcessor_features
    {
        private IMessageHandler messageHandler;
        private JsonMessageSerializer messageSerializer;
        private IMessageProcessingExceptionHandler<BrokeredMessage> exceptionHandler;
        private BrokeredMessageProcessor sut;

        public BrokeredMessageProcessor_features()
        {
            messageHandler = Mock.Of<IMessageHandler>();
            messageSerializer = new JsonMessageSerializer();
            exceptionHandler = Mock.Of<IMessageProcessingExceptionHandler<BrokeredMessage>>();
            sut = new BrokeredMessageProcessor(
                messageHandler,
                messageSerializer,
                exceptionHandler,
                CancellationToken.None);
        }

        [Fact]
        public void class_has_guard_clause()
        {
            var fixture = new Fixture { OmitAutoProperties = true }.Customize(new AutoMoqCustomization());
            var assertion = new GuardClauseAssertion(fixture);
            assertion.Verify(typeof(BrokeredMessageProcessor));
        }

        [Fact]
        public void ProcessMessage_invokes_exception_handler_if_GetBody_fails()
        {
            var brokeredMessage = new BrokeredMessage();
            brokeredMessage.Dispose();

            Func<Task> action = () => sut.ProcessMessage(brokeredMessage);

            action.ShouldThrow<ObjectDisposedException>();
            Mock.Get(exceptionHandler).Verify(
                x =>
                x.Handle(
                    It.Is<MessageProcessingExceptionContext<BrokeredMessage>>(
                        p =>
                        p.Source == brokeredMessage &&
                        p.Body == null &&
                        p.Envelope == null &&
                        p.Exception is ObjectDisposedException &&
                        p.Handled == false)),
                Times.Once());
        }

        [Fact]
        public void ProcessMessage_invokes_exception_handler_if_serialization_fails()
        {
            var body = new byte[] { 1, 2, 3 };
            var brokeredMessage = new BrokeredMessage(new MemoryStream(body));

            Func<Task> action = () => sut.ProcessMessage(brokeredMessage);

            action.ShouldThrow<JsonReaderException>();
            Mock.Get(exceptionHandler).Verify(
                x =>
                x.Handle(
                    It.Is<MessageProcessingExceptionContext<BrokeredMessage>>(
                        p =>
                        p.Source == brokeredMessage &&
                        p.Body != null &&
                        p.Body.SequenceEqual(body) &&
                        p.Envelope == null &&
                        p.Exception is JsonReaderException &&
                        p.Handled == false)),
                Times.Once());
        }

        [Fact]
        public void ProcessMessage_invokes_exception_handler_if_message_handling_fails()
        {
            // Arrange
            var partitionContext = new PartitionContext();

            var correlationId = Guid.NewGuid();
            var envelope = new Envelope(correlationId, "foo");
            string value = messageSerializer.Serialize(envelope);
            byte[] body = Encoding.UTF8.GetBytes(value);
            var brokeredMessage = new BrokeredMessage(new MemoryStream(body));

            Mock.Get(messageHandler)
                .Setup(x => x.Handle(It.IsAny<Envelope>(), CancellationToken.None))
                .Throws<InvalidOperationException>();

            // Act
            Func<Task> action = () => sut.ProcessMessage(brokeredMessage);

            // Assert
            action.ShouldThrow<InvalidOperationException>();
            Mock.Get(exceptionHandler).Verify(
                x =>
                x.Handle(
                    It.Is<MessageProcessingExceptionContext<BrokeredMessage>>(
                        p =>
                        p.Source == brokeredMessage &&
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
        public void ProcessMessage_does_not_throw_if_exception_handler_handles_exception()
        {
            var partitionContext = new PartitionContext();
            var brokeredMessage = new BrokeredMessage();
            brokeredMessage.Dispose();
            Mock.Get(exceptionHandler)
                .Setup(
                    x =>
                    x.Handle(
                        It.IsAny<MessageProcessingExceptionContext<BrokeredMessage>>()))
                .Callback<MessageProcessingExceptionContext<BrokeredMessage>>(p => p.Handled = true)
                .Returns(Task.FromResult(true));

            Func<Task> action = () => sut.ProcessMessage(brokeredMessage);

            action.ShouldNotThrow();
        }

        [Fact]
        public void ProcessMessage_ignores_exception_handler_error()
        {
            var brokeredMessage = new BrokeredMessage();
            brokeredMessage.Dispose();
            Mock.Get(exceptionHandler)
                .Setup(
                    x =>
                    x.Handle(
                        It.IsAny<MessageProcessingExceptionContext<BrokeredMessage>>()))
                .Callback<MessageProcessingExceptionContext<BrokeredMessage>>(p => p.Handled = true)
                .Throws<InvalidOperationException>();

            Func<Task> action = () => sut.ProcessMessage(brokeredMessage);

            action.ShouldNotThrow();
        }
    }
}
