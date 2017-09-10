namespace Khala.Messaging
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using FluentAssertions;
    using Khala.TransientFaultHandling;
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
        public void constructor_sets_properties_correctly()
        {
            var fixture = new Fixture().Customize(new AutoMoqCustomization());
            var retryPolicy = fixture.Create<RetryPolicy>();
            var messageHandler = fixture.Create<IMessageHandler>();

            var sut = new TransientFaultHandlingMessageHandler(retryPolicy, messageHandler);

            sut.RetryPolicy.Should().BeSameAs(retryPolicy);
            sut.MessageHandler.Should().BeSameAs(messageHandler);
        }

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public async Task Handle_relays_to_retry_policy(bool canceled)
        {
            // Arrange
            var cancellationToken = new CancellationToken(canceled);
            var maximumRetryCount = 1;
            var moq = new Mock<RetryPolicy>(
                maximumRetryCount,
                new TransientFaultDetectionStrategy(),
                new ConstantRetryIntervalStrategy(TimeSpan.Zero));
            var messageHandler = Mock.Of<IMessageHandler>();
            var sut = new TransientFaultHandlingMessageHandler(
                moq.Object,
                messageHandler);
            var envelope = new Envelope(new object());

            // Act
            await sut.Handle(envelope, cancellationToken);

            // Assert
            Func<Envelope, CancellationToken, Task> del = messageHandler.Handle;
            moq.Verify(
                x =>
                x.Run<Envelope>(
                    It.Is<Func<Envelope, CancellationToken, Task>>(
                        p =>
                        p.Target == del.Target &&
                        p.Method == del.Method),
                    envelope,
                    cancellationToken),
                Times.Once());
        }
    }
}
