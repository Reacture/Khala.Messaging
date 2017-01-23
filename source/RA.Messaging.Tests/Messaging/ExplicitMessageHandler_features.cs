using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Xunit;

namespace ReactiveArchitecture.Messaging
{
    public class ExplicitMessageHandler_features
    {
        [Fact]
        public void class_is_abstract()
        {
            typeof(ExplicitMessageHandler).IsAbstract.Should().BeTrue();
        }

        [Fact]
        public void sut_implements_IMessageHandler()
        {
            var sut = Mock.Of<ExplicitMessageHandler>();
            sut.Should().BeAssignableTo<IMessageHandler>();
        }

        public class FooMessage
        {
        }

        public class BarMessage
        {
        }

        public abstract class MessageHandler :
            ExplicitMessageHandler,
            IHandles<FooMessage>,
            IHandles<BarMessage>
        {
            public abstract Task Handle(
                Guid messageId,
                Guid? correlationId,
                FooMessage message,
                CancellationToken cancellationToken);

            public abstract Task Handle(
                Guid messageId,
                Guid? correlationId,
                BarMessage message,
                CancellationToken cancellationToken);
        }

        [Fact]
        public async Task sut_invokes_correct_handler_method()
        {
            var mock = new Mock<MessageHandler> { CallBase = true };
            MessageHandler sut = mock.Object;
            object message = new FooMessage();
            Guid correlationId = Guid.NewGuid();
            var envelope = new Envelope(correlationId, message);
            IMessageHandler handler = sut;

            await handler.Handle(envelope, CancellationToken.None);

            Mock.Get(sut).Verify(
                x =>
                x.Handle(
                    envelope.MessageId,
                    correlationId,
                    (FooMessage)message,
                    CancellationToken.None),
                Times.Once());
        }
    }
}
