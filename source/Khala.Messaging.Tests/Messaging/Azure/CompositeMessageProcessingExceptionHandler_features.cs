using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Ploeh.AutoFixture;
using Ploeh.AutoFixture.AutoMoq;
using Ploeh.AutoFixture.Idioms;
using Xunit;

namespace Khala.Messaging.Azure
{
    public class CompositeMessageProcessingExceptionHandler_features
    {
        public class MessageSource
        {
        }

        [Fact]
        public void sut_implements_IMessageProcessingExceptionHandler()
        {
            var sut = new CompositeMessageProcessingExceptionHandler<MessageSource>();
            sut.Should().BeAssignableTo<IMessageProcessingExceptionHandler<MessageSource>>();
        }

        [Fact]
        public void class_has_guard_clauses()
        {
            var fixture = new Fixture().Customize(new AutoMoqCustomization());
            var assertion = new GuardClauseAssertion(fixture);
            assertion.Verify(typeof(CompositeMessageProcessingExceptionHandler<>));
        }

        [Fact]
        public void constructor_has_guard_clause_for_null_handler()
        {
            var handlers = new[]
            {
                Mock.Of<IMessageProcessingExceptionHandler<MessageSource>>(),
                null,
                Mock.Of<IMessageProcessingExceptionHandler<MessageSource>>()
            };

            Action action = () => new CompositeMessageProcessingExceptionHandler<MessageSource>(handlers);

            action.ShouldThrow<ArgumentException>().Where(x => x.ParamName == "handlers");
        }

        [Fact]
        public async Task Handle_invokes_all_handlers()
        {
            var fixture = new Fixture().Customize(new AutoMoqCustomization());
            var handlers = fixture.CreateMany
                <IMessageProcessingExceptionHandler<MessageSource>>();
            var sut = new CompositeMessageProcessingExceptionHandler
                <MessageSource>(handlers.ToArray());
            var context = fixture.Create
                <MessageProcessingExceptionContext<MessageSource>>();

            await sut.Handle(context);

            handlers.ForEach(h => Mock.Get(h).Verify(x => x.Handle(context), Times.Once()));
        }

        [Fact]
        public void Handle_ignores_handler_errors()
        {
            // Arrange
            var fixture = new Fixture().Customize(new AutoMoqCustomization());
            var handlers = fixture.CreateMany
                <IMessageProcessingExceptionHandler<MessageSource>>();
            var context = fixture.Create
                <MessageProcessingExceptionContext<MessageSource>>();
            handlers.Skip(1).Take(1).ForEach(h =>
            {
                Mock.Get(h)
                    .Setup(x => x.Handle(context))
                    .Throws<InvalidOperationException>();
            });
            var sut = new CompositeMessageProcessingExceptionHandler
                <MessageSource>(handlers.ToArray());

            // Act
            Func<Task> action = () => sut.Handle(context);

            // Assert
            action.ShouldNotThrow();
            handlers.ForEach(h => Mock.Get(h).Verify(x => x.Handle(context), Times.Once()));
        }
    }
}
