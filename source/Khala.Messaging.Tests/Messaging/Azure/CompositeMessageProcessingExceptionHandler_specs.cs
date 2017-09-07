namespace Khala.Messaging.Azure
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using FluentAssertions;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Moq;
    using Ploeh.AutoFixture;
    using Ploeh.AutoFixture.AutoMoq;
    using Ploeh.AutoFixture.Idioms;

    [TestClass]
    public class CompositeMessageProcessingExceptionHandler_specs
    {
        [TestMethod]
        public void sut_implements_IMessageProcessingExceptionHandlerT()
        {
            typeof(CompositeMessageProcessingExceptionHandler<MessageData>)
                .Should().Implement(typeof(IMessageProcessingExceptionHandler<MessageData>));
        }

        [TestMethod]
        public void sut_has_guard_clauses()
        {
            var builder = new Fixture().Customize(new AutoMoqCustomization());
            var assertion = new GuardClauseAssertion(builder);
            assertion.Verify(typeof(CompositeMessageProcessingExceptionHandler<>));
        }

        [TestMethod]
        public void constructor_has_guard_clause_against_null_handler()
        {
            var fixture = new Fixture().Customize(new AutoMoqCustomization());
            var random = new Random();
            IMessageProcessingExceptionHandler<MessageData>[] handlers = Enumerable
                .Range(0, 10)
                .Select(_ => fixture.Create<IMessageProcessingExceptionHandler<MessageData>>())
                .Concat(new[] { default(IMessageProcessingExceptionHandler<MessageData>) })
                .OrderBy(_ => random.Next())
                .ToArray();

            Action action = () =>
            new CompositeMessageProcessingExceptionHandler<MessageData>(handlers);

            action.ShouldThrow<ArgumentException>().Where(x => x.ParamName == "handlers");
        }

        [TestMethod]
        public async Task Handle_relays_to_all_inner_handlers()
        {
            var fixture = new Fixture().Customize(new AutoMoqCustomization());
            IMessageProcessingExceptionHandler<MessageData>[] handlers = fixture.CreateMany<IMessageProcessingExceptionHandler<MessageData>>().ToArray();
            var sut = new CompositeMessageProcessingExceptionHandler<MessageData>(handlers);
            var context = fixture.Create<MessageProcessingExceptionContext<MessageData>>();

            await sut.Handle(context);

            foreach (IMessageProcessingExceptionHandler<MessageData> handler in handlers)
            {
                Mock.Get(handler).Verify(x => x.Handle(context), Times.Once());
            }
        }

        [TestMethod]
        public void Handle_consumes_inner_handler_exception()
        {
            var fixture = new Fixture().Customize(new AutoMoqCustomization());
            IMessageProcessingExceptionHandler<MessageData>[] handlers = fixture.CreateMany<IMessageProcessingExceptionHandler<MessageData>>().ToArray();
            var context = fixture.Create<MessageProcessingExceptionContext<MessageData>>();
            Mock.Get(handlers.OrderBy(h => h.GetHashCode()).First())
                .Setup(x => x.Handle(context))
                .Throws<InvalidOperationException>();
            var sut = new CompositeMessageProcessingExceptionHandler<MessageData>(handlers);

            Action action = () => sut.Handle(context);

            action.ShouldNotThrow();
        }

        public class MessageData
        {
        }
    }
}
