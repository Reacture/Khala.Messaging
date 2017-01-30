using System;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Ploeh.AutoFixture;
using Ploeh.AutoFixture.Idioms;
using Ploeh.AutoFixture.Xunit2;
using Xunit;

namespace Khala.Messaging.Azure
{
    public class DelegatingMessageProcessingExceptionHandler_features
    {
        public class MessageSource
        {
        }

        [Fact]
        public void sut_implements_IMessageProcessingExceptionHandler()
        {
            var sut = new DelegatingMessageProcessingExceptionHandler
                <MessageSource>(context => Task.FromResult(true));
            sut.Should().BeAssignableTo
                <IMessageProcessingExceptionHandler<MessageSource>>();
        }

        [Fact]
        public void class_has_guard_clauses()
        {
            var fixture = new Fixture();
            var assertion = new GuardClauseAssertion(fixture);
            assertion.Verify(typeof(DelegatingMessageProcessingExceptionHandler<>));
        }

        public interface IFunctor
        {
            Task Handle(MessageProcessingExceptionContext<MessageSource> context);
        }

        [Theory]
        [AutoData]
        public async Task Handle_invokes_handler(
            MessageProcessingExceptionContext<MessageSource> context)
        {
            var functor = Mock.Of<IFunctor>();
            var sut = new DelegatingMessageProcessingExceptionHandler
                <MessageSource>(functor.Handle);

            await sut.Handle(context);

            Mock.Get(functor).Verify(x => x.Handle(context), Times.Once());
        }

        [Theory]
        [AutoData]
        public void Handle_ignores_handler_error(
            MessageProcessingExceptionContext<MessageSource> context)
        {
            var functor = Mock.Of<IFunctor>();
            Mock.Get(functor)
                .Setup(x => x.Handle(context))
                .Throws<InvalidOperationException>();
            var sut = new DelegatingMessageProcessingExceptionHandler
                <MessageSource>(functor.Handle);

            Func<Task> action = () => sut.Handle(context);

            action.ShouldNotThrow();
        }
    }
}
