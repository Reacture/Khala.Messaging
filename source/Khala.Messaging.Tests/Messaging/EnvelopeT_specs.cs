namespace Khala.Messaging
{
    using System;
    using AutoFixture;
    using AutoFixture.Kernel;
    using FluentAssertions;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Newtonsoft.Json;

    [TestClass]
    public class EnvelopeT_specs
    {
        [TestMethod]
        public void sut_implements_IEnvelope()
        {
            typeof(Envelope<>).Should().Implement<IEnvelope>();
        }

        [TestMethod]
        public void constructor_has_guard_clause_against_empty_message_id()
        {
            Action action = () =>
            new Envelope<Message>(
                messageId: Guid.Empty,
                message: new Message(),
                operationId: default,
                correlationId: default,
                contributor: default);
            action.Should().Throw<ArgumentException>().Where(x => x.ParamName == "messageId");
        }

        [TestMethod]
        public void constructor_has_guard_clause_against_null_message()
        {
            Action action = () =>
            new Envelope<Message>(Guid.NewGuid(), message: null, operationId: default, correlationId: default, contributor: default);
            action.Should().Throw<ArgumentException>().Where(x => x.ParamName == "message");
        }

        [TestMethod]
        public void constructor_has_guard_clause_against_empty_correlation_id()
        {
            Action action = () =>
            new Envelope<Message>(
                messageId: Guid.NewGuid(),
                message: new Message(),
                operationId: default,
                correlationId: Guid.Empty,
                contributor: default);
            action.Should().Throw<ArgumentException>().Where(x => x.ParamName == "correlationId");
        }

        [TestMethod]
        public void sut_is_json_serializable()
        {
            var factory = new MethodInvoker(new GreedyConstructorQuery());
            var builder = new Fixture();
            builder.Customize<Envelope<Message>>(c => c.FromFactory(factory));
            Envelope<Message> sut = builder.Create<Envelope<Message>>();
            string json = JsonConvert.SerializeObject(sut);

            Envelope<Message> actual = JsonConvert.DeserializeObject<Envelope<Message>>(json);

            actual.Should().BeEquivalentTo(sut);
        }

        public class Message
        {
        }
    }
}
