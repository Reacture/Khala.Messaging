namespace Khala.Messaging
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using AutoFixture;
    using AutoFixture.AutoMoq;
    using AutoFixture.Idioms;
    using FluentAssertions;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Moq;

    [TestClass]
    public class InterfaceAwareHandler_specs
    {
        [TestMethod]
        public void class_is_abstract()
        {
            typeof(InterfaceAwareHandler).IsAbstract.Should().BeTrue();
        }

        [TestMethod]
        public void sut_implements_IMessageHandler()
        {
            InterfaceAwareHandler sut = Mock.Of<InterfaceAwareHandler>();
            sut.Should().BeAssignableTo<IMessageHandler>();
        }

        [TestMethod]
        public void sut_has_guard_clauses()
        {
            IFixture builder = new Fixture().Customize(new AutoMoqCustomization());
            var assertion = new GuardClauseAssertion(builder);
            assertion.Verify(typeof(InterfaceAwareHandler));
        }

        public class FooMessage
        {
        }

        public class BarMessage
        {
        }

        public class MessageHandler :
            InterfaceAwareHandler,
            IHandles<FooMessage>,
            IHandles<BarMessage>
        {
            public virtual Task Handle(
                Envelope<FooMessage> envelope,
                CancellationToken cancellationToken)
            {
                return Task.CompletedTask;
            }

            public virtual Task Handle(
                Envelope<BarMessage> envelope,
                CancellationToken cancellationToken)
            {
                return Task.CompletedTask;
            }
        }

        public class UnknownMessage
        {
        }

        [TestMethod]
        public void Accepts_returns_true_if_sut_handles_message()
        {
            var message = new FooMessage();
            var envelope = new Envelope(message);
            var sut = new MessageHandler();

            bool actual = sut.Accepts(envelope);

            actual.Should().BeTrue();
        }

        [TestMethod]
        public void Accepts_returns_false_if_sut_does_not_handle_message()
        {
            var message = new UnknownMessage();
            var envelope = new Envelope(message);
            MessageHandler sut = Mock.Of<MessageHandler>();

            bool actual = sut.Accepts(envelope);

            actual.Should().BeFalse();
        }

        [TestMethod]
        public void Accepts_is_virtual()
        {
            typeof(InterfaceAwareHandler).GetMethod("Accepts").Should().BeVirtual();
        }

        [TestMethod]
        [AutoData]
        public async Task sut_invokes_correct_handler_method(
            Guid messageId,
            FooMessage message,
            string operationId,
            Guid correlationId,
            string contributor)
        {
            var mock = new Mock<MessageHandler> { CallBase = true };
            MessageHandler sut = mock.Object;
            var envelope = new Envelope(messageId, message, operationId, correlationId, contributor);

            await sut.Handle(envelope, CancellationToken.None);

            Mock.Get(sut).Verify(
                x =>
                x.Handle(
                    It.Is<Envelope<FooMessage>>(
                        p =>
                        p.MessageId == messageId &&
                        p.OperationId == operationId &&
                        p.CorrelationId == correlationId &&
                        p.Contributor == contributor &&
                        p.Message == message),
                    CancellationToken.None),
                Times.Once());
        }
    }
}
