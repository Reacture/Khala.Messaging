namespace Khala.Messaging.Azure
{
    using System.Reflection;
    using AutoFixture;
    using AutoFixture.Idioms;
    using FluentAssertions;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class EventProcessingExceptionContext_specs
    {
        [TestMethod]
        public void sut_is_immutable()
        {
            foreach (PropertyInfo property in typeof(EventProcessingExceptionContext).GetProperties())
            {
                property.Should().NotBeWritable();
            }
        }

        [TestMethod]
        public void sut_has_guard_clauses()
        {
            var builder = new Fixture();
            new GuardClauseAssertion(builder).Verify(typeof(EventProcessingExceptionContext));
        }
    }
}
