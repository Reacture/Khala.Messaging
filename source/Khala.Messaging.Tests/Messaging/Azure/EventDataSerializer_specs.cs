namespace Khala.Messaging.Azure
{
    using System;
    using System.Text;
    using FakeBlogEngine;
    using FluentAssertions;
    using Microsoft.Azure.EventHubs;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Ploeh.AutoFixture;
    using Ploeh.AutoFixture.AutoMoq;
    using Ploeh.AutoFixture.Idioms;

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
        public void Serialize_serializes_message_correctly()
        {
            BlogPostCreated message = fixture.Create<BlogPostCreated>();
            var envelope = new Envelope(message);

            EventData eventData = sut.Serialize(envelope);

            ArraySegment<byte> body = eventData.Body;
            string value = Encoding.UTF8.GetString(body.Array, body.Offset, body.Count);
            object actual = messageSerializer.Deserialize(value);
            actual.Should().BeOfType<BlogPostCreated>();
            actual.ShouldBeEquivalentTo(message);
        }

        [TestMethod]
        public void Serialize_sets_MessageId_property_as_string_correctly()
        {
            BlogPostCreated message = fixture.Create<BlogPostCreated>();
            var envelope = new Envelope(message);

            EventData eventData = sut.Serialize(envelope);

            string propertyName = "Khala.Messaging.Envelope.MessageId";
            eventData.Properties.Keys.Should().Contain(propertyName);
            object actual = eventData.Properties[propertyName];
            actual.Should().BeOfType<string>();
            Guid.Parse((string)actual).Should().Be(envelope.MessageId);
        }

        [TestMethod]
        public void Serialize_sets_OperationId_property_as_string_correctly()
        {
            var operationId = Guid.NewGuid();
            BlogPostCreated message = fixture.Create<BlogPostCreated>();
            var envelope = new Envelope(
                messageId: Guid.NewGuid(),
                operationId,
                correlationId: default,
                contributor: default,
                message);

            EventData eventData = sut.Serialize(envelope);

            string propertyName = "Khala.Messaging.Envelope.OperationId";
            eventData.Properties.Keys.Should().Contain(propertyName);
            object actual = eventData.Properties[propertyName];
            actual.Should().BeOfType<string>();
            Guid.Parse((string)actual).Should().Be(operationId);
        }

        [TestMethod]
        public void Serialize_sets_CorrelationId_property_as_string_correctly()
        {
            var correlationId = Guid.NewGuid();
            BlogPostCreated message = fixture.Create<BlogPostCreated>();
            var envelope = new Envelope(
                messageId: Guid.NewGuid(),
                operationId: default,
                correlationId,
                contributor: default,
                message);

            EventData eventData = sut.Serialize(envelope);

            string propertyName = "Khala.Messaging.Envelope.CorrelationId";
            eventData.Properties.Keys.Should().Contain(propertyName);
            object actual = eventData.Properties[propertyName];
            actual.Should().BeOfType<string>();
            Guid.Parse((string)actual).Should().Be(correlationId);
        }

        [TestMethod]
        public void Serialize_sets_Contributor_property_correctly()
        {
            string contributor = Guid.NewGuid().ToString();
            BlogPostCreated message = fixture.Create<BlogPostCreated>();
            var envelope = new Envelope(
                messageId: Guid.NewGuid(),
                operationId: default,
                correlationId: default,
                contributor,
                message);

            EventData eventData = sut.Serialize(envelope);

            string propertyName = "Khala.Messaging.Envelope.Contributor";
            eventData.Properties.Keys.Should().Contain(propertyName);
            object actual = eventData.Properties[propertyName];
            actual.Should().BeOfType<string>().Which.Should().Be(contributor);
        }

        [TestMethod]
        public void Deserialize_deserializes_envelope_correctly()
        {
            var messageId = Guid.NewGuid();
            var operationId = Guid.NewGuid();
            var correlationId = Guid.NewGuid();
            string contributor = Guid.NewGuid().ToString();
            BlogPostCreated message = fixture.Create<BlogPostCreated>();
            var envelope = new Envelope(messageId, operationId, correlationId, contributor, message);
            EventData eventData = sut.Serialize(envelope);

            Envelope actual = sut.Deserialize(eventData);

            actual.ShouldBeEquivalentTo(envelope, opts => opts.RespectingRuntimeTypes());
        }

        [TestMethod]
        public void Deserialize_creates_new_MessageId_if_property_not_set()
        {
            BlogPostCreated message = fixture.Create<BlogPostCreated>();
            var envelope = new Envelope(message);
            EventData eventData = sut.Serialize(envelope);
            eventData.Properties.Remove("Khala.Messaging.Envelope.MessageId");

            Envelope actual = sut.Deserialize(eventData);

            actual.MessageId.Should().NotBeEmpty();
            sut.Deserialize(eventData).MessageId.Should().NotBe(actual.MessageId);
        }

        [TestMethod]
        public void Deserialize_not_fails_even_if_CorrelationId_property_not_set()
        {
            BlogPostCreated message = fixture.Create<BlogPostCreated>();
            var envelope = new Envelope(message);
            EventData eventData = sut.Serialize(envelope);
            eventData.Properties.Remove("Khala.Messaging.Envelope.CorrelationId");

            Envelope actual = null;
            Action action = () => actual = sut.Deserialize(eventData);

            action.ShouldNotThrow();
            actual.ShouldBeEquivalentTo(envelope, opts => opts.RespectingRuntimeTypes());
        }

        [TestMethod]
        public void Deserialize_not_fails_even_if_Contributor_property_not_set()
        {
            BlogPostCreated message = fixture.Create<BlogPostCreated>();
            var envelope = new Envelope(message);
            EventData eventData = sut.Serialize(envelope);
            eventData.Properties.Remove("Khala.Messaging.Envelope.Contributor");

            Envelope actual = null;
            Action action = () => actual = sut.Deserialize(eventData);

            action.ShouldNotThrow();
            actual.ShouldBeEquivalentTo(envelope, opts => opts.RespectingRuntimeTypes());
        }
    }
}
