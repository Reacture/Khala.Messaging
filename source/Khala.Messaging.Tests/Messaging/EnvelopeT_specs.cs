namespace Khala.Messaging
{
    using System;
    using FluentAssertions;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class EnvelopeT_specs
    {
        [TestMethod]
        public void constructor_has_guard_clause_against_empty_correlation_id()
        {
            Action action = () =>
            new Envelope<Message>(Guid.NewGuid(), correlationId: Guid.Empty, contributor: default, message: new Message());
            action.ShouldThrow<ArgumentException>().Where(x => x.ParamName == "correlationId");
        }

        [TestMethod]
        public void constructor_has_guard_clause_against_null_message()
        {
            Action action = () =>
            new Envelope<Message>(Guid.NewGuid(), default, default, message: null);
            action.ShouldThrow<ArgumentException>().Where(x => x.ParamName == "message");
        }

        public class Message
        {
        }
    }
}
