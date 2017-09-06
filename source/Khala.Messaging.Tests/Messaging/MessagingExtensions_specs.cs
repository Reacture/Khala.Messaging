using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Khala.Messaging
{
    [TestClass]
    public class MessagingExtensions_specs
    {
        [TestMethod]
        public void Send_relays_with_none_cancellation_token()
        {
            var task = Task.FromResult(true);
            var envelope = new Envelope(new object());
            var messageBus = Mock.Of<IMessageBus>(
                x => x.Send(envelope, CancellationToken.None) == task);

            Task result = messageBus.Send(envelope);

            Mock.Get(messageBus).Verify(
                x => x.Send(envelope, CancellationToken.None), Times.Once());
            result.Should().BeSameAs(task);
        }

        [TestMethod]
        public void SendBatch_relays_with_none_cancellation_token()
        {
            var task = Task.FromResult(true);
            var envelopes = new Envelope[] { };
            var messageBus = Mock.Of<IMessageBus>(
                x => x.SendBatch(envelopes, CancellationToken.None) == task);

            Task result = messageBus.SendBatch(envelopes);

            Mock.Get(messageBus).Verify(
                x => x.SendBatch(envelopes, CancellationToken.None), Times.Once());
            result.Should().BeSameAs(task);
        }

        [TestMethod]
        public void Handle_relays_with_none_cancellation_token()
        {
            var task = Task.FromResult(true);
            var envelope = new Envelope(new object());
            var messageHandler = Mock.Of<IMessageHandler>(
                x => x.Handle(envelope, CancellationToken.None) == task);

            Task result = messageHandler.Handle(envelope);

            Mock.Get(messageHandler).Verify(
                x => x.Handle(envelope, CancellationToken.None), Times.Once());
            result.Should().BeSameAs(task);
        }
    }
}
