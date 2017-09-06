namespace Khala.Messaging.Azure
{
    using System;
    using FluentAssertions;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Ploeh.AutoFixture;
    using Ploeh.AutoFixture.Idioms;

    [TestClass]
    public class MessageProcessingExceptionContext_specs
    {
        [TestMethod]
        public void modest_constructor_has_guard_clause_against_null_source()
        {
            Exception thrown = null;
            try
            {
                new MessageProcessingExceptionContext<string>(null, new Exception());
            }
            catch (Exception exception)
            {
                thrown = exception;
            }

            thrown.Should().BeOfType<ArgumentNullException>().Which.ParamName.Should().Be("source");
        }

        [TestMethod]
        public void sut_has_guard_clauses()
        {
            new GuardClauseAssertion(new Fixture()).Verify(typeof(MessageProcessingExceptionContext<>));
        }
    }
}
