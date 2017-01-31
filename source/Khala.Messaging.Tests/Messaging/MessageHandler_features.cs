using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Xunit;

namespace Khala.Messaging
{
    public class MessageHandler_features
    {
        [Fact]
        public void class_is_abstract()
        {
            typeof(Messaging.MessageHandler).IsAbstract.Should().BeTrue();
        }

        [Fact]
        public void sut_implements_IMessageHandler()
        {
            var sut = Mock.Of<Messaging.MessageHandler>();
            sut.Should().BeAssignableTo<IMessageHandler>();
        }

        public class FooMessage
        {
        }

        public class BarMessage
        {
        }

        public abstract class MessageHandler :
            Messaging.MessageHandler,
            IHandles<FooMessage>,
            IHandles<BarMessage>
        {
            public abstract Task Handle(
                ReceivedEnvelope<FooMessage> envelope,
                CancellationToken cancellationToken);

            public abstract Task Handle(
                ReceivedEnvelope<BarMessage> envelope,
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

            await sut.Handle(envelope, CancellationToken.None);

            Mock.Get(sut).Verify(
                x =>
                x.Handle(
                    It.Is<ReceivedEnvelope<FooMessage>>(
                        p =>
                        p.MessageId == envelope.MessageId &&
                        p.CorrelationId == correlationId &&
                        p.Message == message),
                    CancellationToken.None),
                Times.Once());
        }
    }
}
