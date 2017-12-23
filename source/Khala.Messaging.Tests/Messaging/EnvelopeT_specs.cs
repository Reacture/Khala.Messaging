namespace Khala.Messaging
{
    using System;
    using FluentAssertions;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class EnvelopeT_specs
    {
        [TestMethod]
        public void constructor_has_guard_clause_against_empty_message_id()
        {
            Action action = () =>
            new Envelope<Message>(
                messageId: Guid.Empty,
                operationId: default,
                correlationId: default,
                contributor: default,
                message: new Message());
            action.ShouldThrow<ArgumentException>().Where(x => x.ParamName == "messageId");
        }

        [TestMethod]
        public void constructor_has_guard_clause_against_empty_operation_id()
        {
            Action action = () =>
            new Envelope<Message>(
                messageId: Guid.NewGuid(),
                operationId: Guid.Empty,
                correlationId: default,
                contributor: default,
                message: new Message());
            action.ShouldThrow<ArgumentException>().Where(x => x.ParamName == "operationId");
        }

        [TestMethod]
        public void constructor_has_guard_clause_against_empty_correlation_id()
        {
            Action action = () =>
            new Envelope<Message>(
                messageId: Guid.NewGuid(),
                operationId: default,
                correlationId: Guid.Empty,
                contributor: default,
                message: new Message());
            action.ShouldThrow<ArgumentException>().Where(x => x.ParamName == "correlationId");
        }

        [TestMethod]
        public void constructor_has_guard_clause_against_null_message()
        {
            Action action = () =>
            new Envelope<Message>(Guid.NewGuid(), default, default, default, message: null);
            action.ShouldThrow<ArgumentException>().Where(x => x.ParamName == "message");
        }

        public class Message
        {
        }
    }
}
