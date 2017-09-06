namespace Khala.Messaging.Azure
{
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

    [TestClass]
    public class BrokeredMessageSerializer_specs
    {
        private readonly IFixture fixture;
        private readonly JsonMessageSerializer messageSerializer;
        private readonly BrokeredMessageSerializer sut;

        public BrokeredMessageSerializer_specs()
        {
            fixture = new Fixture().Customize(new AutoMoqCustomization());
            messageSerializer = new JsonMessageSerializer();
            sut = new BrokeredMessageSerializer(messageSerializer);
        }

        [TestMethod]
        public void sut_implements_IMessageDataSerializer_of_BrokeredMessage()
        {
            typeof(BrokeredMessageSerializer).Should().Implement<IMessageDataSerializer<BrokeredMessage>>();
        }

        [TestMethod]
        public void class_has_guard_clauses()
        {
            fixture.OmitAutoProperties = true;
            var assertion = new GuardClauseAssertion(fixture);
            assertion.Verify(typeof(BrokeredMessageSerializer));
        }

        [TestMethod]
        public async Task Serialize_serializes_message_correctly()
        {
            var message = fixture.Create<BlogPostCreated>();
            var envelope = new Envelope(message);

            BrokeredMessage brokeredMessage = await sut.Serialize(envelope);

            using (var stream = brokeredMessage.GetBody<Stream>())
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

            BrokeredMessage brokeredMessage = await sut.Serialize(envelope);

            brokeredMessage.MessageId.Should().Be(envelope.MessageId.ToString("n"));
            string propertyName = "Khala.Messaging.Envelope.MessageId";
            brokeredMessage.Properties.Keys.Should().Contain(propertyName);
            object actual = brokeredMessage.Properties[propertyName];
            actual.Should().BeOfType<string>();
            Guid.Parse((string)actual).Should().Be(envelope.MessageId);
        }

        [TestMethod]
        public async Task Serialize_sets_CorrelationId_property_as_string_correctly()
        {
            var correlationId = Guid.NewGuid();
            var message = fixture.Create<BlogPostCreated>();
            var envelope = new Envelope(correlationId, message);

            BrokeredMessage brokeredMessage = await sut.Serialize(envelope);

            brokeredMessage.CorrelationId.Should().Be(envelope.CorrelationId?.ToString("n"));
            string propertyName = "Khala.Messaging.Envelope.CorrelationId";
            brokeredMessage.Properties.Keys.Should().Contain(propertyName);
            object actual = brokeredMessage.Properties[propertyName];
            actual.Should().BeOfType<string>();
            Guid.Parse((string)actual).Should().Be(correlationId);
        }

        [TestMethod]
        public async Task Serialize_sets_PartitionKey_correctly_if_message_is_IPartitioned()
        {
            IPartitioned message = fixture.Create<BlogPostCreated>();
            var envelope = new Envelope(message);
            BrokeredMessage brokeredMessage = await sut.Serialize(envelope);
            brokeredMessage.PartitionKey.Should().Be(message.PartitionKey);
        }

        [TestMethod]
        public async Task Deserialize_deserializes_envelope_correctly()
        {
            var correlationId = Guid.NewGuid();
            var message = fixture.Create<BlogPostCreated>();
            var envelope = new Envelope(correlationId, message);
            BrokeredMessage brokeredMessage = await sut.Serialize(envelope);

            Envelope actual = await sut.Deserialize(brokeredMessage);

            actual.ShouldBeEquivalentTo(
                envelope, opts => opts.RespectingRuntimeTypes());
        }

        [TestMethod]
        public async Task Deserialize_creates_new_MessageId_if_property_not_set()
        {
            var message = fixture.Create<BlogPostCreated>();
            var envelope = new Envelope(message);
            BrokeredMessage brokeredMessage = await sut.Serialize(envelope);
            brokeredMessage.Properties.Remove("Khala.Messaging.Envelope.MessageId");

            Envelope actual = await sut.Deserialize(brokeredMessage.Clone());

            actual.MessageId.Should().NotBeEmpty();
            (await sut.Deserialize(brokeredMessage)).MessageId.Should().NotBe(actual.MessageId);
        }

        [TestMethod]
        public async Task Deserialize_not_fails_even_if_CorrelationId_property_not_set()
        {
            var message = fixture.Create<BlogPostCreated>();
            var envelope = new Envelope(message);
            BrokeredMessage brokeredMessage = await sut.Serialize(envelope);
            brokeredMessage.Properties.Remove("Khala.Envelope.CorrelationId");

            Envelope actual = null;
            Func<Task> action = async () =>
            actual = await sut.Deserialize(brokeredMessage);

            action.ShouldNotThrow();
            actual.ShouldBeEquivalentTo(
                envelope, opts => opts.RespectingRuntimeTypes());
        }
    }
}
