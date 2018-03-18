namespace Khala.Messaging
{
    using System;
    using FluentAssertions;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class Envelope_specs
    {
        [TestMethod]
        public void sut_implements_IEnvelope()
        {
            typeof(Envelope).Should().Implement<IEnvelope>();
        }

        [TestMethod]
        public void constructor_has_guard_clause_against_empty_messageId()
        {
            Action action = () =>
            new Envelope(messageId: Guid.Empty, message: new object());
            action.Should().Throw<ArgumentException>()
                .Where(x => x.ParamName == "messageId");
        }

        [TestMethod]
        public void constructor_has_guard_clause_against_empty_operationId()
        {
            Action action = () =>
            new Envelope(Guid.NewGuid(), new object(), operationId: Guid.Empty);
            action.Should().Throw<ArgumentException>()
                .Where(x => x.ParamName == "operationId");
        }

        [TestMethod]
        public void constructor_allows_null_operationId()
        {
            Action action = () =>
            new Envelope(Guid.NewGuid(), new object(), operationId: null);
            action.Should().NotThrow();
        }

        [TestMethod]
        public void constructor_has_guard_clause_against_empty_correlationId()
        {
            Action action = () =>
            new Envelope(Guid.NewGuid(), new object(), correlationId: Guid.Empty);
            action.Should().Throw<ArgumentException>()
                .Where(x => x.ParamName == "correlationId");
        }

        [TestMethod]
        public void constructor_allows_null_correlationId()
        {
            Action action = () =>
            new Envelope(Guid.NewGuid(), new object(), correlationId: null);
            action.Should().NotThrow();
        }
    }
}
