namespace Khala.Messaging.Azure
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Ploeh.AutoFixture;
    using Ploeh.AutoFixture.AutoMoq;
    using Ploeh.AutoFixture.Idioms;

    [TestClass]
    public class EventMessageProcessorFactory_specs
    {
        [TestMethod]
        public void sut_has_guard_clauses()
        {
            var builder = new Fixture().Customize(new AutoMoqCustomization());
            var assertion = new GuardClauseAssertion(builder);
            assertion.Verify(typeof(EventMessageProcessorFactory));
        }
    }
}
