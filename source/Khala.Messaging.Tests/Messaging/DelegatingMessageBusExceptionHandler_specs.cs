namespace Khala.Messaging
{
    using System;
    using System.Threading.Tasks;
    using FluentAssertions;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Moq;
    using Ploeh.AutoFixture;
    using Ploeh.AutoFixture.Idioms;

    [TestClass]
    public class DelegatingMessageBusExceptionHandler_specs
    {
        public interface IFunctionProvider
        {
            TResult Func<T, TResult>(T arg);
        }

        [TestMethod]
        public void sut_implements_IMessageBusExceptionHandler()
        {
            typeof(DelegatingMessageBusExceptionHandler).Should().Implement<IMessageBusExceptionHandler>();
        }

        [TestMethod]
        public async Task Handle_relays_to_function()
        {
            var functionProvider = Mock.Of<IFunctionProvider>();
            var sut = new DelegatingMessageBusExceptionHandler(functionProvider.Func<MessageBusExceptionContext, Task>);
            var context = new MessageBusExceptionContext(new[] { new Envelope(new object()) }, new Exception());

            await sut.Handle(context);

            Mock.Get(functionProvider).Verify(x => x.Func<MessageBusExceptionContext, Task>(context), Times.Once());
        }

        [TestMethod]
        public void sut_has_guard_clauses()
        {
            var builder = new Fixture();
            new GuardClauseAssertion(builder).Verify(typeof(DelegatingMessageBusExceptionHandler));
        }
    }
}
