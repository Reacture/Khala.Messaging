namespace Khala.Messaging
{
    using System.Threading;
    using System.Threading.Tasks;
    using AutoFixture;
    using AutoFixture.AutoMoq;
    using AutoFixture.Idioms;
    using FluentAssertions;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Moq;

    [TestClass]
    public class MessagingExtensions_specs
    {
        [TestMethod]
        public void sut_has_guard_clauses()
        {
            IFixture builder = new Fixture().Customize(new AutoMoqCustomization());
            new GuardClauseAssertion(builder).Verify(typeof(MessagingExtensions));
        }

        [TestMethod]
        public void Send_relays_with_none_cancellation_token()
        {
            var task = Task.FromResult(true);
            var envelope = new Envelope(new object());
            IMessageBus messageBus = Mock.Of<IMessageBus>(
                x => x.Send(envelope, CancellationToken.None) == task);

            Task result = messageBus.Send(envelope);

            Mock.Get(messageBus).Verify(x => x.Send(envelope, CancellationToken.None), Times.Once());
            result.Should().BeSameAs(task);
        }

        [TestMethod]
        public void Send_with_envelopes_relays_with_none_cancellation_token()
        {
            var task = Task.FromResult(true);
            var envelopes = new Envelope[] { };
            IMessageBus messageBus = Mock.Of<IMessageBus>(
                x => x.Send(envelopes, CancellationToken.None) == task);

            Task result = messageBus.Send(envelopes);

            Mock.Get(messageBus).Verify(x => x.Send(envelopes, CancellationToken.None), Times.Once());
            result.Should().BeSameAs(task);
        }

        [TestMethod]
        public void Handle_relays_with_none_cancellation_token()
        {
            var task = Task.FromResult(true);
            var envelope = new Envelope(new object());
            IMessageHandler messageHandler = Mock.Of<IMessageHandler>(
                x => x.Handle(envelope, CancellationToken.None) == task);

            Task result = messageHandler.Handle(envelope);

            Mock.Get(messageHandler).Verify(x => x.Handle(envelope, CancellationToken.None), Times.Once());
            result.Should().BeSameAs(task);
        }

        [TestMethod]
        [AutoData]
        public void HandleTMessage_releys_with_non_cancellation_token(Envelope<object> envelope)
        {
            var task = Task.FromResult(true);
            IHandles<object> handles = Mock.Of<IHandles<object>>(
                x => x.Handle(envelope, CancellationToken.None) == task);

            Task result = handles.Handle(envelope);

            Mock.Get(handles).Verify(x => x.Handle(envelope, CancellationToken.None), Times.Once());
            result.Should().BeSameAs(task);
        }
    }
}
