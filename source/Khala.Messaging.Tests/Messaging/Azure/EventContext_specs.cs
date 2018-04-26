namespace Khala.Messaging.Azure
{
    using System.Collections.Generic;
    using System.Reflection;
    using AutoFixture;
    using AutoFixture.AutoMoq;
    using AutoFixture.Idioms;
    using FluentAssertions;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class EventContext_specs
    {
        [TestMethod]
        public void sut_is_immutable()
        {
            PropertyInfo[] properties = typeof(EventContext).GetProperties();
            foreach (PropertyInfo property in properties)
            {
                property.Should().NotBeWritable();
            }
        }

        [TestMethod]
        public void sut_is_sealed()
        {
            typeof(EventContext).IsSealed.Should().BeTrue();
        }

        [TestMethod]
        public void constructor_sets_properties_correctly()
        {
            var envelope = new Envelope(new object());
            IDictionary<string, object> properties = new Dictionary<string, object>();

            var sut = new EventContext(envelope, properties);

            sut.Envelope.Should().BeSameAs(envelope);
            sut.Properties.Should().BeSameAs(properties);
        }

        [TestMethod]
        public void constructor_has_guard_clauses()
        {
            IFixture builder = new Fixture().Customize(new AutoMoqCustomization());
            new GuardClauseAssertion(builder).Verify(typeof(EventContext));
        }
    }
}
