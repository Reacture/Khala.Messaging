namespace Khala.Messaging
{
    using System;
    using FluentAssertions;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

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

        public class Message
        {
        }
    }
}
