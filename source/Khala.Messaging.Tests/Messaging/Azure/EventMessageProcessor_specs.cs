namespace Khala.Messaging.Azure
{
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using AutoFixture;
    using AutoFixture.AutoMoq;
    using AutoFixture.Idioms;
    using FluentAssertions;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Moq;

    [TestClass]
    public class EventMessageProcessor_specs
    {
        [TestMethod]
        public void sut_has_guard_clauses()
        {
            IFixture builder = new Fixture().Customize(new AutoMoqCustomization());
            new GuardClauseAssertion(builder).Verify(typeof(EventMessageProcessor));
        }

        [TestMethod]
        public void Process_invokes_message_handler_correctly()
        {
            var envelope = new Envelope(new object());
            var properties = new Dictionary<string, object>();
            CancellationToken cancellationToken = new CancellationTokenSource().Token;
            Task task = Task.FromResult(new object());
            IMessageHandler messageHandler = Mock.Of<IMessageHandler>(
                x =>
                x.Accepts(envelope) == true &&
                x.Handle(envelope, cancellationToken) == task);
            var sut = new EventMessageProcessor(messageHandler);

            Task actual = sut.Process(envelope, properties, cancellationToken);

            actual.Should().Be(task);
        }

        [TestMethod]
        public async Task Process_invokes_message_handler_even_if_message_is_unacceptable()
        {
            var envelope = new Envelope(new object());
            var properties = new Dictionary<string, object>();
            CancellationToken cancellationToken = new CancellationTokenSource().Token;
            IMessageHandler messageHandler = Mock.Of<IMessageHandler>(
                x =>
                x.Accepts(envelope) == false);
            var sut = new EventMessageProcessor(messageHandler);

            await sut.Process(envelope, properties, cancellationToken);

            Mock.Get(messageHandler)
                .Verify(x => x.Handle(envelope, cancellationToken), Times.Once());
        }
    }
}
