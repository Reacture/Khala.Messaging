namespace Khala.Messaging
{
    using System;
    using FluentAssertions;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Ploeh.AutoFixture;
    using Ploeh.AutoFixture.Idioms;

    [TestClass]
    public class Envelope_specs
    {
        [TestMethod]
        public void class_has_guard_clause()
        {
            var fixture = new Fixture();
            var assertion = new GuardClauseAssertion(fixture);
            assertion.Verify(typeof(Envelope));
        }

        [TestMethod]
        public void constructor_has_guard_clause_for_empty_correlationId()
        {
            Action action = () =>
            new Envelope(Guid.NewGuid(), Guid.Empty, new object());
            action.ShouldThrow<ArgumentException>()
                .Where(x => x.ParamName == "correlationId");
        }

        [TestMethod]
        public void constructor_allows_null_correlationId()
        {
            Action action = () =>
            new Envelope(Guid.NewGuid(), null, new object());
            action.ShouldNotThrow();
        }
    }
}
