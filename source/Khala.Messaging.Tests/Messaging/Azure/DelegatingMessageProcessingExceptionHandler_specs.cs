namespace Khala.Messaging.Azure
{
    using System;
    using System.Threading.Tasks;
    using FluentAssertions;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Moq;
    using Ploeh.AutoFixture;
    using Ploeh.AutoFixture.Idioms;

    [TestClass]
    public class DelegatingMessageProcessingExceptionHandler_specs
    {
        [TestMethod]
        public void sut_has_guard_clauses()
        {
            var fixture = new Fixture();
            var assertion = new GuardClauseAssertion(fixture);
            assertion.Verify(typeof(DelegatingMessageProcessingExceptionHandler<>));
        }

        [TestMethod]
        public void Handle_relays_to_handler_function_correctly()
        {
            // Arrange
            var functionProvider = Mock.Of<IFunctionProvider>();
            var fixture = new Fixture();
            var source = fixture.Create<string>();
            var exception = fixture.Create<Exception>();
            var sut = new DelegatingMessageProcessingExceptionHandler<string>(
                functionProvider.Func<MessageProcessingExceptionContext<string>, Task>);
            var context = new MessageProcessingExceptionContext<string>(source, exception);

            // Act
            sut.Handle(context);

            // Assert
            Mock.Get(functionProvider).Verify(x => x.Func<MessageProcessingExceptionContext<string>, Task>(context), Times.Once());
        }

        [TestMethod]
        public void Handle_consumes_handler_function_error()
        {
            var sut = new DelegatingMessageProcessingExceptionHandler<string>(
                x => throw new InvalidOperationException());

            Func<Task> action = () =>
            sut.Handle(new MessageProcessingExceptionContext<string>("foo", new Exception()));

            action.ShouldNotThrow();
        }

        public interface IFunctionProvider
        {
            TResult Func<T, TResult>(T arg);
        }
    }
}
