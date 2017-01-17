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
                FooMessage message,
                CancellationToken cancellationToken);

            public abstract Task Handle(
                BarMessage message,
                CancellationToken cancellationToken);
        }

        [Fact]
        public async Task sut_invokes_correct_handler_method()
        {
            var mock = new Mock<MessageHandler> { CallBase = true };
            IMessageHandler sut = mock.Object;
            object message = new FooMessage();

            await sut.Handle(message, CancellationToken.None);

            Mock.Get(sut).Verify(
                x =>
                x.Handle((FooMessage)message, CancellationToken.None),
                Times.Once());
            Mock.Get(sut).Verify(
                x =>
                x.Handle(It.IsAny<BarMessage>(), It.IsAny<CancellationToken>()),
                Times.Never());
        }
    }
}
