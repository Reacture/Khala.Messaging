using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.ServiceBus.Messaging;
using Ploeh.AutoFixture;
using Ploeh.AutoFixture.AutoMoq;
using Ploeh.AutoFixture.Idioms;
using Xunit;

namespace Khala.Messaging.Azure
{
    public class EventDataSerializer_features
    {
        private IFixture fixture;
        private IMessageSerializer messageSerializer;
        private EventDataSerializer sut;

        public EventDataSerializer_features()
        {
            fixture = new Fixture().Customize(new AutoMoqCustomization());
            messageSerializer = new JsonMessageSerializer();
            sut = new EventDataSerializer(messageSerializer);
        }

        [Fact]
        public void class_has_guard_clauses()
        {
            var assertion = new GuardClauseAssertion(fixture);
            assertion.Verify(typeof(EventDataSerializer));
        }

        public class FakeMessage
        {
            public Guid EntityId { get; set; }

            public string Property { get; set; }
        }

        [Fact]
        public async Task Serialize_serializes_message_correctly()
        {
            var message = fixture.Create<FakeMessage>();
            var envelope = new Envelope(message);

            EventData eventData = await sut.Serialize(envelope);

            using (Stream stream = eventData.GetBodyStream())
            using (var reader = new StreamReader(stream, Encoding.UTF8))
            {
                string value = reader.ReadToEnd();
                object actual = messageSerializer.Deserialize(value);
                actual.Should().BeOfType<FakeMessage>();
                actual.ShouldBeEquivalentTo(message);
            }
        }

        [Fact]
        public async Task Serialize_sets_MessageId_property_as_string_correctly()
        {
            var message = fixture.Create<FakeMessage>();
            var envelope = new Envelope(message);

            EventData eventData = await sut.Serialize(envelope);

            string propertyName = "Khala.Envelope.MessageId";
            eventData.Properties.Keys.Should().Contain(propertyName);
            object actual = eventData.Properties[propertyName];
            actual.Should().BeOfType<string>();
            Guid.Parse((string)actual).Should().Be(envelope.MessageId);
        }

        [Fact]
        public async Task Serialize_sets_CorrelationId_property_as_string_correctly()
        {
            var correlationId = Guid.NewGuid();
            var message = fixture.Create<FakeMessage>();
            var envelope = new Envelope(correlationId, message);

            EventData eventData = await sut.Serialize(envelope);

            string propertyName = "Khala.Envelope.CorrelationId";
            eventData.Properties.Keys.Should().Contain(propertyName);
            object actual = eventData.Properties[propertyName];
            actual.Should().BeOfType<string>();
            Guid.Parse((string)actual).Should().Be(correlationId);
        }

        [Fact]
        public async Task Deserialize_deserializes_envelope_correctly()
        {
            var correlationId = Guid.NewGuid();
            var message = fixture.Create<FakeMessage>();
            var envelope = new Envelope(correlationId, message);
            EventData eventData = await sut.Serialize(envelope);

            Envelope actual = await sut.Deserialize(eventData);

            actual.ShouldBeEquivalentTo(
                envelope, opts => opts.RespectingRuntimeTypes());
        }

        [Fact]
        public async Task Deserialize_creates_new_MessageId_if_property_not_set()
        {
            var message = fixture.Create<FakeMessage>();
            var envelope = new Envelope(message);
            EventData eventData = await sut.Serialize(envelope);
            eventData.Properties.Remove("Khala.Envelope.MessageId");

            Envelope actual = await sut.Deserialize(eventData.Clone());

            actual.MessageId.Should().NotBeEmpty();
            (await sut.Deserialize(eventData)).MessageId.Should().NotBe(actual.MessageId);
        }

        [Fact]
        public async Task Deserialize_not_fails_even_if_CorrelationId_property_not_set()
        {
            var message = fixture.Create<FakeMessage>();
            var envelope = new Envelope(message);
            EventData eventData = await sut.Serialize(envelope);
            eventData.Properties.Remove("Khala.Envelope.CorrelationId");

            Envelope actual = null;
            Func<Task> action = async () =>
            actual = await sut.Deserialize(eventData);

            action.ShouldNotThrow();
            actual.ShouldBeEquivalentTo(
                envelope, opts => opts.RespectingRuntimeTypes());
        }
    }
}
