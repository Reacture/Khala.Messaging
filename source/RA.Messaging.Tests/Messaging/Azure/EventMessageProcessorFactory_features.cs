using FluentAssertions;
using Microsoft.ServiceBus.Messaging;
using Ploeh.AutoFixture;
using Ploeh.AutoFixture.AutoMoq;
using Ploeh.AutoFixture.Idioms;
using Xunit;

namespace ReactiveArchitecture.Messaging.Azure
{
    public class EventMessageProcessorFactory_features
    {
        [Fact]
        public void class_has_guard_clauses()
        {
            var fixture = new Fixture().Customize(new AutoMoqCustomization());
            var assertion = new GuardClauseAssertion(fixture);
            assertion.Verify(typeof(EventMessageProcessorFactory));
        }

        [Fact]
        public void CreateEventProcessor_creates_EventMessageProcessor()
        {
            var fixture = new Fixture().Customize(new AutoMoqCustomization());
            var sut = fixture.Create<EventMessageProcessorFactory>();
            var context = new PartitionContext();

            IEventProcessor actual = sut.CreateEventProcessor(context);

            actual.Should().BeOfType<EventMessageProcessor>();
        }
    }
}
