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

        [TestMethod]
        public void Handled_has_false_initial_value()
        {
            var sut = new MessageProcessingExceptionContext<string>("foo", new Exception());
            sut.Handled.Should().BeFalse();
        }

        [TestMethod]
        public void constructor_sets_Source_correctly()
        {
            var source = new Fixture().Create<string>();
            var sut = new MessageProcessingExceptionContext<string>(source, new Exception());
            sut.Source.Should().Be(source);
        }

        [TestMethod]
        public void constructor_sets_Envelope_correctly()
        {
            var envelope = new Fixture().Create<Envelope>();
            var sut = new MessageProcessingExceptionContext<string>("foo", envelope, new Exception());
            sut.Envelope.Should().BeSameAs(envelope);
        }

        [TestMethod]
        public void constructor_sets_Exception_correctly()
        {
            var exception = new Fixture().Create<Exception>();
            var sut = new MessageProcessingExceptionContext<string>("foo", new Envelope(new object()), exception);
            sut.Exception.Should().BeSameAs(exception);
        }
    }
}
