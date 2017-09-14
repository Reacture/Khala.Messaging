namespace Khala.Messaging
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using FluentAssertions;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Ploeh.AutoFixture;
    using Ploeh.AutoFixture.Idioms;

    [TestClass]
    public class MessageBusExceptionContext_specs
    {
        [TestMethod]
        public void sut_has_Envelopes_property()
        {
            typeof(MessageBusExceptionContext).Should().HaveProperty<IReadOnlyList<Envelope>>("Envelopes");
        }

        [TestMethod]
        public void Envelopes_does_not_have_setter()
        {
            typeof(MessageBusExceptionContext).GetProperty("Envelopes").Should().NotBeWritable();
        }

        [TestMethod]
        public void Exception_does_not_have_setter()
        {
            typeof(MessageBusExceptionContext).GetProperty("Exception").Should().NotBeWritable();
        }

        [TestMethod]
        public void constructor_has_guard_clause()
        {
            var builder = new Fixture();
            ConstructorInfo constructor = typeof(MessageBusExceptionContext)
                .GetConstructor(new[] { typeof(IEnumerable<Envelope>), typeof(Exception) });
            new GuardClauseAssertion(builder).Verify(constructor);
        }

        [TestMethod]
        public void sut_initializes_Handled_to_false()
        {
            var sut = new MessageBusExceptionContext(
                new[] { new Envelope(new object()) },
                new Exception());

            sut.Handled.Should().BeFalse();
        }

        [TestMethod]
        public void constructor_sets_Envelopes_correctly()
        {
            var fixture = new Fixture();
            IEnumerable<Envelope> envelopes = fixture.CreateMany<Envelope>();

            var sut = new MessageBusExceptionContext(envelopes, new Exception());

            sut.Envelopes.Should().Equal(envelopes);
        }

        [TestMethod]
        public void constructor_has_guard_clause_against_empty_envelopes()
        {
            var envelopes = new Envelope[] { };
            Action action = () => new MessageBusExceptionContext(envelopes, new Exception());
            action.ShouldThrow<ArgumentException>().Where(x => x.ParamName == "envelopes");
        }

        [TestMethod]
        public void constructor_has_guard_clause_against_null_envelope()
        {
            var envelopes = new[] { new Envelope(new object()), new Envelope(new object()), null };
            var random = new Random();

            Action action = () =>
            new MessageBusExceptionContext(
                from e in envelopes
                orderby random.Next()
                select e,
                new Exception());

            action.ShouldThrow<ArgumentException>().Where(x => x.ParamName == "envelopes");
        }
    }
}
