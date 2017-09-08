namespace Khala.Messaging
{
    using System.Threading;
    using System.Threading.Tasks;
    using FluentAssertions;
    using Khala.TransientFaultHandling;
    using Khala.TransientFaultHandling.Testing;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Moq;
    using Ploeh.AutoFixture;
    using Ploeh.AutoFixture.AutoMoq;
    using Ploeh.AutoFixture.Idioms;

    [TestClass]
    public class TransientFaultHandlingMessageHandler_specs
    {
        [TestMethod]
        public void sut_implements_IMessageHandler()
        {
            typeof(TransientFaultHandlingMessageHandler).Should().Implement<IMessageHandler>();
        }

        [TestMethod]
        public void sut_has_guard_clauses()
        {
            var builder = new Fixture().Customize(new AutoMoqCustomization());
            new GuardClauseAssertion(builder).Verify(typeof(TransientFaultHandlingMessageHandler));
        }

        [TestMethod]
        public async Task Handle_relays_with_retry_policy()
        {
            // Arrange
            var spy = new TransientFaultHandlingSpy();
            RetryPolicy retryPolicy = spy.Policy;
            var envelope = new Envelope(new object());
            var messageHandler = Mock.Of<IMessageHandler>();
            Mock.Get(messageHandler)
                .Setup(x => x.Handle(envelope, CancellationToken.None))
                .Callback<Envelope, CancellationToken>((envelopeParam, cancellationToken) => spy.OperationCancellable.Invoke(cancellationToken))
                .Returns(Task.FromResult(true));
            var sut = new TransientFaultHandlingMessageHandler(retryPolicy, messageHandler);

            // Act
            await sut.Handle(envelope);

            // Assert
            spy.Verify();
            Mock.Get(messageHandler).Verify(x => x.Handle(envelope, CancellationToken.None));
        }
    }
}
