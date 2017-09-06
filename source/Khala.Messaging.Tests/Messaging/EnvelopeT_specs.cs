namespace Khala.Messaging
{
    using System;
    using FluentAssertions;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Ploeh.AutoFixture;
    using Ploeh.AutoFixture.Idioms;

    [TestClass]
    public class EnvelopeT_specs
    {
        [TestMethod]
        public void sut_has_guard_clauses()
        {
            new GuardClauseAssertion(new Fixture()).Verify(typeof(Envelope<>));
        }

        [TestMethod]
        public void constructor_has_guard_clause_against_empty_correlation_id()
        {
            Action action = () =>
            new Envelope<Message>(Guid.NewGuid(), correlationId: Guid.Empty, message: new Message());
            action.ShouldThrow<ArgumentException>().Where(x => x.ParamName == "correlationId");
        }

        [TestMethod]
        public void constructor_has_guard_clause_against_null_message()
        {
            Action action = () =>
            new Envelope<Message>(Guid.NewGuid(), null, message: null);
            action.ShouldThrow<ArgumentException>().Where(x => x.ParamName == "message");
        }

        public class Message
        {
        }
    }
}
