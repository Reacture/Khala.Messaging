namespace Khala.Messaging.Azure
{
    using System;
    using System.Text;
    using AutoFixture;
    using AutoFixture.AutoMoq;
    using AutoFixture.Idioms;
    using FakeBlogEngine;
    using FluentAssertions;
    using Microsoft.Azure.EventHubs;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

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
            actual.Should().BeEquivalentTo(message);
        }

        [TestMethod]
        public void Serialize_sets_MessageId_property_correctly()
        {
            BlogPostCreated message = fixture.Create<BlogPostCreated>();
            var envelope = new Envelope(message);

            EventData eventData = sut.Serialize(envelope);

            string propertyName = nameof(Envelope.MessageId);
            eventData.Properties.Keys.Should().Contain(propertyName);
            object actual = eventData.Properties[propertyName];
            actual.Should().Be(envelope.MessageId);
        }

        [TestMethod]
        public void Serialize_sets_OperationId_property_correctly()
        {
            var operationId = Guid.NewGuid();
            BlogPostCreated message = fixture.Create<BlogPostCreated>();
            var envelope = new Envelope(
                messageId: Guid.NewGuid(),
                message,
                operationId);

            EventData eventData = sut.Serialize(envelope);

            string propertyName = nameof(Envelope.OperationId);
            eventData.Properties.Keys.Should().Contain(propertyName);
            object actual = eventData.Properties[propertyName];
            actual.Should().Be(operationId);
        }

        [TestMethod]
        public void Serialize_sets_CorrelationId_property_correctly()
        {
            var correlationId = Guid.NewGuid();
            BlogPostCreated message = fixture.Create<BlogPostCreated>();
            var envelope = new Envelope(
                messageId: Guid.NewGuid(),
                message,
                correlationId: correlationId);

            EventData eventData = sut.Serialize(envelope);

            string propertyName = nameof(Envelope.CorrelationId);
            eventData.Properties.Keys.Should().Contain(propertyName);
            object actual = eventData.Properties[propertyName];
            actual.Should().Be(correlationId);
        }

        [TestMethod]
        public void Serialize_sets_Contributor_property_correctly()
        {
            string contributor = Guid.NewGuid().ToString();
            BlogPostCreated message = fixture.Create<BlogPostCreated>();
            var envelope = new Envelope(
                messageId: Guid.NewGuid(),
                message,
                contributor: contributor);

            EventData eventData = sut.Serialize(envelope);

            string propertyName = nameof(Envelope.Contributor);
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
            var envelope = new Envelope(messageId, message, operationId, correlationId, contributor);
            EventData eventData = sut.Serialize(envelope);

            Envelope actual = sut.Deserialize(eventData);

            actual.Should().BeEquivalentTo(envelope, opts => opts.RespectingRuntimeTypes());
        }

        [TestMethod]
        public void Deserialize_creates_new_MessageId_if_property_not_set()
        {
            BlogPostCreated message = fixture.Create<BlogPostCreated>();
            var envelope = new Envelope(message);
            EventData eventData = sut.Serialize(envelope);
            eventData.Properties.Remove("MessageId");

            Envelope actual = sut.Deserialize(eventData);

            actual.MessageId.Should().NotBeEmpty();
            sut.Deserialize(eventData).MessageId.Should().NotBe(actual.MessageId);
        }

        [TestMethod]
        public void Deserialize_not_fails_even_if_OperationId_property_not_set()
        {
            BlogPostCreated message = fixture.Create<BlogPostCreated>();
            var envelope = new Envelope(message);
            EventData eventData = sut.Serialize(envelope);
            eventData.Properties.Remove("OperationId");

            Envelope actual = null;
            Action action = () => actual = sut.Deserialize(eventData);

            action.Should().NotThrow();
            actual.Should().BeEquivalentTo(envelope, opts => opts.RespectingRuntimeTypes());
        }

        [TestMethod]
        public void Deserialize_not_fails_even_if_CorrelationId_property_not_set()
        {
            BlogPostCreated message = fixture.Create<BlogPostCreated>();
            var envelope = new Envelope(message);
            EventData eventData = sut.Serialize(envelope);
            eventData.Properties.Remove("CorrelationId");

            Envelope actual = null;
            Action action = () => actual = sut.Deserialize(eventData);

            action.Should().NotThrow();
            actual.Should().BeEquivalentTo(envelope, opts => opts.RespectingRuntimeTypes());
        }

        [TestMethod]
        public void Deserialize_not_fails_even_if_Contributor_property_not_set()
        {
            BlogPostCreated message = fixture.Create<BlogPostCreated>();
            var envelope = new Envelope(message);
            EventData eventData = sut.Serialize(envelope);
            eventData.Properties.Remove("Contributor");

            Envelope actual = null;
            Action action = () => actual = sut.Deserialize(eventData);

            action.Should().NotThrow();
            actual.Should().BeEquivalentTo(envelope, opts => opts.RespectingRuntimeTypes());
        }
    }
}
