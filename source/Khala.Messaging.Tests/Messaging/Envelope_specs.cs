namespace Khala.Messaging
{
    using System;
    using FluentAssertions;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class Envelope_specs
    {
        [TestMethod]
        public void constructor_has_guard_clause_against_empty_messageId()
        {
            Action action = () =>
            new Envelope(messageId: Guid.Empty, default, default, default, new object());
            action.ShouldThrow<ArgumentException>()
                .Where(x => x.ParamName == "messageId");
        }

        [TestMethod]
        public void constructor_has_guard_clause_against_empty_operationId()
        {
            Action action = () =>
            new Envelope(Guid.NewGuid(), operationId: Guid.Empty, default, default, new object());
            action.ShouldThrow<ArgumentException>()
                .Where(x => x.ParamName == "operationId");
        }

        [TestMethod]
        public void constructor_allows_null_operationId()
        {
            Action action = () =>
            new Envelope(Guid.NewGuid(), operationId: null, default, default, new object());
            action.ShouldNotThrow();
        }

        [TestMethod]
        public void constructor_has_guard_clause_against_empty_correlationId()
        {
            Action action = () =>
            new Envelope(Guid.NewGuid(), default, correlationId: Guid.Empty, default, new object());
            action.ShouldThrow<ArgumentException>()
                .Where(x => x.ParamName == "correlationId");
        }

        [TestMethod]
        public void constructor_allows_null_correlationId()
        {
            Action action = () =>
            new Envelope(Guid.NewGuid(), default, correlationId: null, default, new object());
            action.ShouldNotThrow();
        }
    }
}
