using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Ploeh.AutoFixture;
using Ploeh.AutoFixture.AutoMoq;
using Ploeh.AutoFixture.Idioms;
using Xunit;

namespace ReactiveArchitecture.Messaging
{
    public class CompositeMessageHandler_features
    {
        [Fact]
        public void sut_implements_IMessageHandler()
        {
            var sut = new CompositeMessageHandler();
            sut.Should().BeAssignableTo<IMessageHandler>();
        }

        [Fact]
        public void class_has_guard_clauses()
        {
            var fixture = new Fixture().Customize(new AutoMoqCustomization());
            var assertion = new GuardClauseAssertion(fixture);
            assertion.Verify(typeof(CompositeMessageHandler));
        }

        [Fact]
        public void constructor_has_guard_clause_for_null_handler()
        {
            Action action = () => new CompositeMessageHandler(
                Mock.Of<IMessageHandler>(),
                default(IMessageHandler),
                Mock.Of<IMessageHandler>());

            action.ShouldThrow<ArgumentException>()
                .Where(x => x.ParamName == "handlers");
        }

        [Fact]
        public async Task Handle_sends_message_to_all_handlers()
        {
            var handler1 = Mock.Of<IMessageHandler>();
            var handler2 = Mock.Of<IMessageHandler>();
            var sut = new CompositeMessageHandler(handler1, handler2);
            var message = new object();
            var envelope = new Envelope(message);

            await sut.Handle(envelope, CancellationToken.None);

            Mock.Get(handler1).Verify(
                x => x.Handle(envelope, CancellationToken.None), Times.Once());
            Mock.Get(handler2).Verify(
                x => x.Handle(envelope, CancellationToken.None), Times.Once());
        }

        [Fact]
        public async Task Handle_sends_message_to_all_handlers_even_if_some_handler_fails()
        {
            var handler1 = Mock.Of<IMessageHandler>();
            var handler2 = Mock.Of<IMessageHandler>();
            var sut = new CompositeMessageHandler(handler1, handler2);
            var message = new object();
            var envelope = new Envelope(message);
            Mock.Get(handler1)
                .Setup(x => x.Handle(envelope, CancellationToken.None))
                .Throws<InvalidOperationException>();

            try
            {
                await sut.Handle(envelope, CancellationToken.None);
            }
            catch
            {
            }

            Mock.Get(handler1).Verify(
                x => x.Handle(envelope, CancellationToken.None), Times.Once());
            Mock.Get(handler2).Verify(
                x => x.Handle(envelope, CancellationToken.None), Times.Once());
        }

        [Fact]
        public void Handle_throws_aggregate_exception_if_handler_fails()
        {
            // Arrange
            var handler1 = Mock.Of<IMessageHandler>();
            var handler2 = Mock.Of<IMessageHandler>();

            var sut = new CompositeMessageHandler(handler1, handler2);

            var message = new object();
            var envelope = new Envelope(message);

            var exception1 = new InvalidOperationException();
            Mock.Get(handler1)
                .Setup(x => x.Handle(envelope, CancellationToken.None))
                .Throws(exception1);

            var exception2 = new InvalidOperationException();
            Mock.Get(handler2)
                .Setup(x => x.Handle(envelope, CancellationToken.None))
                .Throws(exception2);

            // Act
            Func<Task> action = () => sut.Handle(envelope, CancellationToken.None);

            // Arrange
            action.ShouldThrow<AggregateException>()
                .Which.InnerExceptions.Should().ContainSingle()
                .Which.Should().BeOfType<AggregateException>()
                .Which.InnerExceptions
                .ShouldAllBeEquivalentTo(new[] { exception1, exception2 });
        }
    }
}
