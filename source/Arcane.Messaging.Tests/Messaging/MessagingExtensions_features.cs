using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Xunit;

namespace Arcane.Messaging
{
    public class MessagingExtensions_features
    {
        [Fact]
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

        [Fact]
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
    }
}
