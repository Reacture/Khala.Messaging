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
            new Envelope(Guid.Empty, default, default, new object());
            action.ShouldThrow<ArgumentException>()
                .Where(x => x.ParamName == "messageId");
        }

        [TestMethod]
        public void constructor_has_guard_clause_against_empty_correlationId()
        {
            Action action = () =>
            new Envelope(Guid.NewGuid(), Guid.Empty, default, new object());
            action.ShouldThrow<ArgumentException>()
                .Where(x => x.ParamName == "correlationId");
        }

        [TestMethod]
        public void constructor_allows_null_correlationId()
        {
            Guid? correlationId = null;
            Action action = () =>
            new Envelope(Guid.NewGuid(), correlationId, default, new object());
            action.ShouldNotThrow();
        }
    }
}
