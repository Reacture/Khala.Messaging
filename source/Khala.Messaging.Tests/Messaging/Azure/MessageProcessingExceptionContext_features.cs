using System;
using FluentAssertions;
using Ploeh.AutoFixture;
using Ploeh.AutoFixture.AutoMoq;
using Ploeh.AutoFixture.Idioms;
using Xunit;

namespace Khala.Messaging.Azure
{
    public class MessageProcessingExceptionContext_features
    {
        [Fact]
        public void class_has_guard_clauses()
        {
            var fixture = new Fixture().Customize(new AutoMoqCustomization());
            var assertion = new GuardClauseAssertion(fixture);
            assertion.Verify(typeof(MessageProcessingExceptionContext<>));
        }

        [Fact]
        public void Default_value_of_Handled_is_false()
        {
            var sut = new MessageProcessingExceptionContext<object>(
                new object(),
                new byte[0],
                new Envelope(new object()),
                new Exception());
            sut.Handled.Should().BeFalse();
        }
    }
}
