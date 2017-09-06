using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using FakeBlogEngine;
using FluentAssertions;
using Microsoft.ServiceBus.Messaging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ploeh.AutoFixture;
using Ploeh.AutoFixture.AutoMoq;
using Ploeh.AutoFixture.Idioms;

namespace Khala.Messaging.Azure
{
    [TestClass]
    public class EventDataSerializer_specs
    {
        private IFixture fixture;
        private IMessageSerializer messageSerializer;
        private EventDataSerializer sut;

        [TestInitialize]
        public void TestInitialize()
        {
            fixture = new Fixture().Customize(new AutoMoqCustomization());
            messageSerializer = new JsonMessageSerializer();
            sut = new EventDataSerializer(messageSerializer);
        }

        [TestMethod]
        public void class_has_guard_clauses()
        {
            var assertion = new GuardClauseAssertion(fixture);
            assertion.Verify(typeof(EventDataSerializer));
        }

        [TestMethod]
        public async Task Serialize_serializes_message_correctly()
        {
            var message = fixture.Create<BlogPostCreated>();
            var envelope = new Envelope(message);

            EventData eventData = await sut.Serialize(envelope);

            using (Stream stream = eventData.GetBodyStream())
            using (var reader = new StreamReader(stream, Encoding.UTF8))
            {
                string value = reader.ReadToEnd();
                object actual = messageSerializer.Deserialize(value);
                actual.Should().BeOfType<BlogPostCreated>();
                actual.ShouldBeEquivalentTo(message);
            }
        }

        [TestMethod]
        public async Task Serialize_sets_MessageId_property_as_string_correctly()
        {
            var message = fixture.Create<BlogPostCreated>();
            var envelope = new Envelope(message);

            EventData eventData = await sut.Serialize(envelope);

            string propertyName = "Khala.Messaging.Envelope.MessageId";
            eventData.Properties.Keys.Should().Contain(propertyName);
            object actual = eventData.Properties[propertyName];
            actual.Should().BeOfType<string>();
            Guid.Parse((string)actual).Should().Be(envelope.MessageId);
        }

        [TestMethod]
        public async Task Serialize_sets_CorrelationId_property_as_string_correctly()
        {
            var correlationId = Guid.NewGuid();
            var message = fixture.Create<BlogPostCreated>();
            var envelope = new Envelope(correlationId, message);

            EventData eventData = await sut.Serialize(envelope);

            string propertyName = "Khala.Messaging.Envelope.CorrelationId";
            eventData.Properties.Keys.Should().Contain(propertyName);
            object actual = eventData.Properties[propertyName];
            actual.Should().BeOfType<string>();
            Guid.Parse((string)actual).Should().Be(correlationId);
        }

        [TestMethod]
        public async Task Serialize_sets_PartitionKey_correctly_if_message_is_IPartitioned()
        {
            IPartitioned message = fixture.Create<BlogPostCreated>();
            var envelope = new Envelope(message);
            EventData eventData = await sut.Serialize(envelope);
            eventData.PartitionKey.Should().Be(message.PartitionKey);
        }

        [TestMethod]
        public async Task Deserialize_deserializes_envelope_correctly()
        {
            var correlationId = Guid.NewGuid();
            var message = fixture.Create<BlogPostCreated>();
            var envelope = new Envelope(correlationId, message);
            EventData eventData = await sut.Serialize(envelope);

            Envelope actual = await sut.Deserialize(eventData);

            actual.ShouldBeEquivalentTo(
                envelope, opts => opts.RespectingRuntimeTypes());
        }

        [TestMethod]
        public async Task Deserialize_creates_new_MessageId_if_property_not_set()
        {
            var message = fixture.Create<BlogPostCreated>();
            var envelope = new Envelope(message);
            EventData eventData = await sut.Serialize(envelope);
            eventData.Properties.Remove("Khala.Messaging.Envelope.MessageId");

            Envelope actual = await sut.Deserialize(eventData.Clone());

            actual.MessageId.Should().NotBeEmpty();
            (await sut.Deserialize(eventData)).MessageId.Should().NotBe(actual.MessageId);
        }

        [TestMethod]
        public async Task Deserialize_not_fails_even_if_CorrelationId_property_not_set()
        {
            var message = fixture.Create<BlogPostCreated>();
            var envelope = new Envelope(message);
            EventData eventData = await sut.Serialize(envelope);
            eventData.Properties.Remove("Khala.Messaging.Envelope.CorrelationId");

            Envelope actual = null;
            Func<Task> action = async () =>
            actual = await sut.Deserialize(eventData);

            action.ShouldNotThrow();
            actual.ShouldBeEquivalentTo(
                envelope, opts => opts.RespectingRuntimeTypes());
        }
    }
}
