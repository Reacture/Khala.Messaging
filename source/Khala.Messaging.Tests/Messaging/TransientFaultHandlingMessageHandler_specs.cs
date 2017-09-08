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
        public async Task Handle_relays_with_retry_policy(bool canceled)
        {
            // Arrange
            var cancellationToken = new CancellationToken(canceled);
            var functionProvider = Mock.Of<IFunctionProvider>();
            var spy = new TransientFaultHandlingActionSpy<Envelope>(functionProvider.Action);
            var sut = new TransientFaultHandlingMessageHandler(
                spy.Policy,
                new DelegatingMessageHandler(spy.Operation));
            var envelope = new Envelope(new object());

            // Act
            await sut.Handle(envelope, cancellationToken);

            // Assert
            spy.Verify();
            Mock.Get(functionProvider).Verify(x => x.Action(envelope, cancellationToken));
        }

        public interface IFunctionProvider
        {
            void Action<T1, T2>(T1 arg1, T2 arg2);
        }
    }
}
