namespace Khala.Messaging
{
    using System;
    using AutoFixture;
    using AutoFixture.Idioms;
    using FluentAssertions;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class ScheduledEnvelope_specs
    {
        [TestMethod]
        public void sut_is_sealed()
        {
            typeof(ScheduledEnvelope).IsSealed.Should().BeTrue();
        }

        [TestMethod]
        public void sut_has_Envelope_property()
        {
            typeof(ScheduledEnvelope)
                .Should()
                .HaveProperty<Envelope>("Envelope")
                .Which.Should().NotBeWritable();
        }

        [TestMethod]
        public void sut_has_ScheduledTime_property()
        {
            typeof(ScheduledEnvelope)
                .Should()
                .HaveProperty<DateTimeOffset>("ScheduledTime")
                .Which.Should().NotBeWritable();
        }

        [TestMethod]
        public void sut_has_guard_clauses()
        {
            var builder = new Fixture();
            new GuardClauseAssertion(builder).Verify(typeof(ScheduledEnvelope));
        }

        [TestMethod]
        public void constructor_sets_properties_correctly()
        {
            var fixture = new Fixture();
            Envelope envelope = fixture.Create<Envelope>();
            DateTimeOffset scheduledTime = fixture.Create<DateTimeOffset>();

            var sut = new ScheduledEnvelope(envelope, scheduledTime);

            sut.Envelope.Should().BeSameAs(envelope);
            sut.ScheduledTime.Should().Be(scheduledTime);
        }
    }
}
