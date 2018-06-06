namespace Khala.Messaging.Azure
{
    using System;
    using System.Text;
    using AutoFixture.Idioms;
    using FluentAssertions;
    using Microsoft.Azure.EventHubs;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class EventDataSerializer_specs
    {
        public class Message
        {
            public int Foo { get; set; }

            public string Bar { get; set; }
        }

        [TestMethod]
        [AutoData]
        public void class_has_guard_clauses(GuardClauseAssertion assertion)
        {
            assertion.Verify(typeof(EventDataSerializer));
        }

        [TestMethod]
        [AutoData]
        public void Serialize_serializes_message_correctly(Message message, JsonMessageSerializer serializer)
        {
            var sut = new EventDataSerializer(serializer);
            var envelope = new Envelope(message);

            EventData eventData = sut.Serialize(envelope);

            ArraySegment<byte> body = eventData.Body;
            string value = Encoding.UTF8.GetString(body.Array, body.Offset, body.Count);
            object actual = serializer.Deserialize(value);
            actual.Should().BeOfType<Message>();
            actual.Should().BeEquivalentTo(message);
        }

        [TestMethod]
        [AutoData]
        public void Serialize_sets_MessageId_property_correctly(EventDataSerializer sut, Message message)
        {
            var envelope = new Envelope(message);

            EventData eventData = sut.Serialize(envelope);

            string propertyName = nameof(Envelope.MessageId);
            eventData.Properties.Keys.Should().Contain(propertyName);
            object actual = eventData.Properties[propertyName];
            actual.Should().Be(envelope.MessageId);
        }

        [TestMethod]
        [AutoData]
        public void Serialize_sets_OperationId_property_correctly(
            EventDataSerializer sut, Guid messageId, Message message, string operationId)
        {
            var envelope = new Envelope(messageId, message, operationId);

            EventData eventData = sut.Serialize(envelope);

            string propertyName = nameof(Envelope.OperationId);
            eventData.Properties.Keys.Should().Contain(propertyName);
            object actual = eventData.Properties[propertyName];
            actual.Should().Be(operationId);
        }

        [TestMethod]
        [AutoData]
        public void Serialize_sets_CorrelationId_property_correctly(
            EventDataSerializer sut, Guid messageId, Message message, Guid correlationId)
        {
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
        [AutoData]
        public void Serialize_sets_Contributor_property_correctly(
            EventDataSerializer sut, Guid messageId, Message message, string contributor)
        {
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
        [AutoData]
        public void Deserialize_deserializes_envelope_correctly(
            EventDataSerializer sut,
            Guid messageId,
            Message message,
            string operationId,
            Guid correlationId,
            string contributor)
        {
            var envelope = new Envelope(messageId, message, operationId, correlationId, contributor);
            EventData eventData = sut.Serialize(envelope);

            Envelope actual = sut.Deserialize(eventData);

            actual.Should().BeEquivalentTo(envelope, opts => opts.RespectingRuntimeTypes());
        }

        [TestMethod]
        [AutoData]
        public void Deserialize_creates_new_MessageId_if_property_not_set(
            EventDataSerializer sut, Message message)
        {
            var envelope = new Envelope(message);
            EventData eventData = sut.Serialize(envelope);
            eventData.Properties.Remove("MessageId");

            Envelope actual = sut.Deserialize(eventData);

            actual.MessageId.Should().NotBeEmpty();
            sut.Deserialize(eventData).MessageId.Should().NotBe(actual.MessageId);
        }

        [TestMethod]
        [AutoData]
        public void Deserialize_not_fails_even_if_OperationId_property_not_set(
            EventDataSerializer sut, Message message)
        {
            var envelope = new Envelope(message);
            EventData eventData = sut.Serialize(envelope);
            eventData.Properties.Remove("OperationId");

            Envelope actual = null;
            Action action = () => actual = sut.Deserialize(eventData);

            action.Should().NotThrow();
            actual.Should().BeEquivalentTo(envelope, opts => opts.RespectingRuntimeTypes());
        }

        [TestMethod]
        [AutoData]
        public void Deserialize_not_fails_even_if_CorrelationId_property_not_set(
            EventDataSerializer sut, Message message)
        {
            var envelope = new Envelope(message);
            EventData eventData = sut.Serialize(envelope);
            eventData.Properties.Remove("CorrelationId");

            Envelope actual = null;
            Action action = () => actual = sut.Deserialize(eventData);

            action.Should().NotThrow();
            actual.Should().BeEquivalentTo(envelope, opts => opts.RespectingRuntimeTypes());
        }

        [TestMethod]
        [AutoData]
        public void Deserialize_not_fails_even_if_Contributor_property_not_set(
            EventDataSerializer sut, Message message)
        {
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
