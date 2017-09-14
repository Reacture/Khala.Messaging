namespace Khala.Messaging
{
    using System.Threading.Tasks;
    using FluentAssertions;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class IMessageBusExceptionHandler_specs
    {
        [TestMethod]
        public void sut_is_abstract()
        {
            typeof(IMessageBusExceptionHandler).IsAbstract.Should().BeTrue(because: "the class should be abstract.");
        }

        [TestMethod]
        public void sut_has_Handle_asynchronous_method()
        {
            typeof(IMessageBusExceptionHandler)
                .Should()
                .HaveMethod("Handle", new[] { typeof(MessageBusExceptionContext) })
                .Which.Should()
                .Match(x => x.ReturnType == typeof(Task));
        }
    }
}
