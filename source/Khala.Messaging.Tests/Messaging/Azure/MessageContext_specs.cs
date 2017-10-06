namespace Khala.Messaging.Azure
{
    using System.Threading.Tasks;
    using FluentAssertions;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Moq;
    using Ploeh.AutoFixture;
    using Ploeh.AutoFixture.Idioms;

    [TestClass]
    public class MessageContext_specs
    {
        [TestMethod]
        public void constructor_sets_data_correctly()
        {
            var data = new Data();
            var sut = new MessageContext<Data>(data, () => Task.CompletedTask);
            sut.Data.Should().BeSameAs(data);
        }

        [TestMethod]
        public void sut_has_guard_clauses()
        {
            var builder = new Fixture();
            new GuardClauseAssertion(builder).Verify(typeof(MessageContext<>));
        }

        [TestMethod]
        public void Acknowledge_invokes_ack_function_once()
        {
            var data = new Data();
            var functionProvider = Mock.Of<IFunctionProvider>();
            var sut = new MessageContext<Data>(data, functionProvider.Func);

            sut.Acknowledge();

            Mock.Get(functionProvider).Verify(x => x.Func(), Times.Once());
        }

        public interface IFunctionProvider
        {
            Task Func();
        }

        public class Data
        {
        }
    }
}
