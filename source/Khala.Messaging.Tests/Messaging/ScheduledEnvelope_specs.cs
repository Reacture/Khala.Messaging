namespace Khala.Messaging
{
    using System;
    using FluentAssertions;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Ploeh.AutoFixture;
    using Ploeh.AutoFixture.Idioms;

    [TestClass]
    public class ScheduledEnvelope_specs
    {
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
            var envelope = fixture.Create<Envelope>();
            var scheduledTime = fixture.Create<DateTimeOffset>();

            var sut = new ScheduledEnvelope(envelope, scheduledTime);

            sut.Envelope.Should().BeSameAs(envelope);
            sut.ScheduledTime.Should().Be(scheduledTime);
        }
    }
}
