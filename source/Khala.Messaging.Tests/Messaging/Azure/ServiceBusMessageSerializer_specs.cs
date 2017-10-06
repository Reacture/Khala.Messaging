namespace Khala.Messaging.Azure
{
    using System;
    using System.Text;
    using System.Threading.Tasks;
    using FakeBlogEngine;
    using FluentAssertions;
    using Microsoft.Azure.ServiceBus;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Ploeh.AutoFixture;
    using Ploeh.AutoFixture.AutoMoq;
    using Ploeh.AutoFixture.Idioms;

    [TestClass]
    public class ServiceBusMessageSerializer_specs
    {
        private readonly IFixture fixture;
        private readonly JsonMessageSerializer messageSerializer;
        private readonly ServiceBusMessageSerializer sut;

        public ServiceBusMessageSerializer_specs()
        {
            fixture = new Fixture().Customize(new AutoMoqCustomization());
            messageSerializer = new JsonMessageSerializer();
            sut = new ServiceBusMessageSerializer(messageSerializer);
        }

        [TestMethod]
        public void sut_implements_IMessageDataSerializer_of_BrokeredMessage()
        {
            typeof(ServiceBusMessageSerializer).Should().Implement<IMessageDataSerializer<Message>>();
        }

        [TestMethod]
        public void class_has_guard_clauses()
        {
            fixture.OmitAutoProperties = true;
            var assertion = new GuardClauseAssertion(fixture);
            assertion.Verify(typeof(ServiceBusMessageSerializer));
        }

        [TestMethod]
        public async Task Serialize_serializes_message_correctly()
        {
            var message = fixture.Create<BlogPostCreated>();
            var envelope = new Envelope(message);

            Message serviceBusMessage = await sut.Serialize(envelope);

            string value = Encoding.UTF8.GetString(serviceBusMessage.Body);
            object actual = messageSerializer.Deserialize(value);
            actual.Should().BeOfType<BlogPostCreated>();
            actual.ShouldBeEquivalentTo(message);
        }

        [TestMethod]
        public async Task Serialize_sets_MessageId_property_as_string_correctly()
        {
            var message = fixture.Create<BlogPostCreated>();
            var envelope = new Envelope(message);

            Message serviceBusMessage = await sut.Serialize(envelope);

            serviceBusMessage.MessageId.Should().Be(envelope.MessageId.ToString("n"));
            string propertyName = "Khala.Messaging.Envelope.MessageId";
            serviceBusMessage.UserProperties.Keys.Should().Contain(propertyName);
            object actual = serviceBusMessage.UserProperties[propertyName];
            actual.Should().BeOfType<string>();
            Guid.Parse((string)actual).Should().Be(envelope.MessageId);
        }

        [TestMethod]
        public async Task Serialize_sets_CorrelationId_property_as_string_correctly()
        {
            var correlationId = Guid.NewGuid();
            var message = fixture.Create<BlogPostCreated>();
            var envelope = new Envelope(correlationId, message);

            Message serviceBusMessage = await sut.Serialize(envelope);

            serviceBusMessage.CorrelationId.Should().Be(envelope.CorrelationId?.ToString("n"));
            string propertyName = "Khala.Messaging.Envelope.CorrelationId";
            serviceBusMessage.UserProperties.Keys.Should().Contain(propertyName);
            object actual = serviceBusMessage.UserProperties[propertyName];
            actual.Should().BeOfType<string>();
            Guid.Parse((string)actual).Should().Be(correlationId);
        }

        [TestMethod]
        public async Task Serialize_sets_PartitionKey_correctly_if_message_is_IPartitioned()
        {
            IPartitioned message = fixture.Create<BlogPostCreated>();
            var envelope = new Envelope(message);
            Message serviceBusMessage = await sut.Serialize(envelope);
            serviceBusMessage.PartitionKey.Should().Be(message.PartitionKey);
        }

        [TestMethod]
        public async Task Serialize_sets_PartitionKey_to_null_if_message_is_not_IPartitioned()
        {
            var message = new { Value = 1024 };
            var envelope = new Envelope(message);

            Message serviceBusMessage = await sut.Serialize(envelope);

            serviceBusMessage.PartitionKey.Should().BeNull();
        }

        [TestMethod]
        public async Task Deserialize_deserializes_envelope_correctly()
        {
            var correlationId = Guid.NewGuid();
            var message = fixture.Create<BlogPostCreated>();
            var envelope = new Envelope(correlationId, message);
            Message serviceBusMessage = await sut.Serialize(envelope);

            Envelope actual = await sut.Deserialize(serviceBusMessage);

            actual.ShouldBeEquivalentTo(envelope, opts => opts.RespectingRuntimeTypes());
        }

        [TestMethod]
        public async Task Deserialize_creates_new_MessageId_if_property_not_set()
        {
            var message = fixture.Create<BlogPostCreated>();
            var envelope = new Envelope(message);
            Message serviceBusMessage = await sut.Serialize(envelope);
            serviceBusMessage.UserProperties.Remove("Khala.Messaging.Envelope.MessageId");

            Envelope actual = await sut.Deserialize(serviceBusMessage.Clone());

            actual.MessageId.Should().NotBeEmpty();
            (await sut.Deserialize(serviceBusMessage)).MessageId.Should().NotBe(actual.MessageId);
        }

        [TestMethod]
        public async Task Deserialize_not_fails_even_if_CorrelationId_property_not_set()
        {
            var message = fixture.Create<BlogPostCreated>();
            var envelope = new Envelope(message);
            Message serviceBusMessage = await sut.Serialize(envelope);
            serviceBusMessage.UserProperties.Remove("Khala.Envelope.CorrelationId");

            Envelope actual = null;
            Func<Task> action = async () =>
            actual = await sut.Deserialize(serviceBusMessage);

            action.ShouldNotThrow();
            actual.ShouldBeEquivalentTo(envelope, opts => opts.RespectingRuntimeTypes());
        }
    }
}
