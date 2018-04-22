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
    public class CompositeMessageHandler_specs
    {
        [TestMethod]
        public void sut_implements_IMessageHandler()
        {
            var sut = new CompositeMessageHandler();
            sut.Should().BeAssignableTo<IMessageHandler>();
        }

        [TestMethod]
        public void class_has_guard_clauses()
        {
            IFixture builder = new Fixture().Customize(new AutoMoqCustomization());
            new GuardClauseAssertion(builder).Verify(typeof(CompositeMessageHandler));
        }

        [TestMethod]
        public void constructor_has_guard_clause_for_null_handler()
        {
            Action action = () => new CompositeMessageHandler(
                Mock.Of<IMessageHandler>(),
                default(IMessageHandler),
                Mock.Of<IMessageHandler>());

            action.Should().Throw<ArgumentException>()
                .Where(x => x.ParamName == "handlers");
        }

        [TestMethod]
        public void constructor_sets_Handles_correctly()
        {
            IMessageHandler handler1 = Mock.Of<IMessageHandler>();
            IMessageHandler handler2 = Mock.Of<IMessageHandler>();
            IMessageHandler handler3 = Mock.Of<IMessageHandler>();

            var sut = new CompositeMessageHandler(handler1, handler2, handler3);

            sut.Handlers.Should().Equal(handler1, handler2, handler3);
        }

        [TestMethod]
        public void Accepts_returns_true_if_all_handlers_accept_message()
        {
            Envelope envelope = new Fixture().Create<Envelope>();
            IMessageHandler handler1 = Mock.Of<IMessageHandler>(x => x.Accepts(envelope) == true);
            IMessageHandler handler2 = Mock.Of<IMessageHandler>(x => x.Accepts(envelope) == true);
            IMessageHandler handler3 = Mock.Of<IMessageHandler>(x => x.Accepts(envelope) == true);
            var sut = new CompositeMessageHandler(handler1, handler2, handler3);

            bool actual = sut.Accepts(envelope);

            actual.Should().BeTrue();
        }

        [TestMethod]
        [DataRow(true, false, false)]
        [DataRow(false, true, false)]
        [DataRow(false, false, true)]
        [DataRow(true, true, false)]
        [DataRow(true, false, true)]
        [DataRow(false, true, true)]
        public void Accepts_returns_true_if_some_handlers_accept_message(bool accepts1, bool accepts2, bool accepts3)
        {
            Envelope envelope = new Fixture().Create<Envelope>();
            IMessageHandler handler1 = Mock.Of<IMessageHandler>(x => x.Accepts(envelope) == accepts1);
            IMessageHandler handler2 = Mock.Of<IMessageHandler>(x => x.Accepts(envelope) == accepts2);
            IMessageHandler handler3 = Mock.Of<IMessageHandler>(x => x.Accepts(envelope) == accepts3);
            var sut = new CompositeMessageHandler(handler1, handler2, handler3);

            bool actual = sut.Accepts(envelope);

            actual.Should().BeTrue();
        }

        [TestMethod]
        public void Accepts_returns_false_if_no_handler_accepts_message()
        {
            Envelope envelope = new Fixture().Create<Envelope>();
            IMessageHandler handler1 = Mock.Of<IMessageHandler>(x => x.Accepts(envelope) == false);
            IMessageHandler handler2 = Mock.Of<IMessageHandler>(x => x.Accepts(envelope) == false);
            IMessageHandler handler3 = Mock.Of<IMessageHandler>(x => x.Accepts(envelope) == false);
            var sut = new CompositeMessageHandler(handler1, handler2, handler3);

            bool actual = sut.Accepts(envelope);

            actual.Should().BeFalse();
        }

        [TestMethod]
        public async Task Handle_sends_message_to_all_handlers()
        {
            IMessageHandler handler1 = Mock.Of<IMessageHandler>();
            IMessageHandler handler2 = Mock.Of<IMessageHandler>();
            var sut = new CompositeMessageHandler(handler1, handler2);
            object message = new object();
            var envelope = new Envelope(message);

            await sut.Handle(envelope, CancellationToken.None);

            Mock.Get(handler1).Verify(
                x => x.Handle(envelope, CancellationToken.None), Times.Once());
            Mock.Get(handler2).Verify(
                x => x.Handle(envelope, CancellationToken.None), Times.Once());
        }

        [TestMethod]
        public async Task Handle_sends_message_to_all_handlers_even_if_some_handler_fails()
        {
            IMessageHandler handler1 = Mock.Of<IMessageHandler>();
            IMessageHandler handler2 = Mock.Of<IMessageHandler>();
            var sut = new CompositeMessageHandler(handler1, handler2);
            object message = new object();
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

        [TestMethod]
        public void Handle_throws_aggregate_exception_if_handler_fails()
        {
            // Arrange
            IMessageHandler handler1 = Mock.Of<IMessageHandler>();
            IMessageHandler handler2 = Mock.Of<IMessageHandler>();

            var sut = new CompositeMessageHandler(handler1, handler2);

            object message = new object();
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
            action.Should().Throw<AggregateException>()
                .Which.InnerExceptions
                .Should().BeEquivalentTo(exception1, exception2);
        }
    }
}
