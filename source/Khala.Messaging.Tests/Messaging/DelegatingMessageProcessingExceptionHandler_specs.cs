namespace Khala.Messaging
{
    using System;
    using System.Threading.Tasks;
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
            var builder = new Fixture();
            new GuardClauseAssertion(builder).Verify(typeof(DelegatingMessageProcessingExceptionHandler<>));
        }

        [TestMethod]
        public void Handle_relays_to_handler_function_correctly()
        {
            // Arrange
            var fixture = new Fixture();
            var functionProvider = Mock.Of<IFunctionProvider>();
            var sut = new DelegatingMessageProcessingExceptionHandler<string>(functionProvider.Func);
            var data = fixture.Create<string>();
            var exception = fixture.Create<Exception>();
            var context = new MessageProcessingExceptionContext<string>(data, exception);

            // Act
            sut.Handle(context);

            // Assert
            Mock.Get(functionProvider).Verify(x => x.Func(context), Times.Once());
        }

        public interface IFunctionProvider
        {
            Task Func<T>(T arg);
        }
    }
}
