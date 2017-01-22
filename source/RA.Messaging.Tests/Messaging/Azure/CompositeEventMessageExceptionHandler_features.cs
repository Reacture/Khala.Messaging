using System;
using FluentAssertions;
using Moq;
using Ploeh.AutoFixture;
using Ploeh.AutoFixture.AutoMoq;
using Ploeh.AutoFixture.Idioms;
using Xunit;

namespace ReactiveArchitecture.Messaging.Azure
{
    public class CompositeEventMessageExceptionHandler_features
    {
        [Fact]
        public void sut_implements_IEventMessageExceptionHandler()
        {
            var sut = new CompositeEventMessageExceptionHandler();
            sut.Should().BeAssignableTo<IEventMessageExceptionHandler>();
        }

        [Fact]
        public void class_has_guard_clauses()
        {
            var fixture = new Fixture().Customize(new AutoMoqCustomization());
            var assertion = new GuardClauseAssertion(fixture);
            assertion.Verify(typeof(CompositeEventMessageExceptionHandler));
        }

        [Fact]
        public void constructor_has_guard_clause_for_null_exception_handler()
        {
            var exceptionHandlers = new IEventMessageExceptionHandler[]
            {
                Mock.Of<IEventMessageExceptionHandler>(),
                null,
                Mock.Of<IEventMessageExceptionHandler>()
            };

            Action action = () => new CompositeEventMessageExceptionHandler(exceptionHandlers);

            action.ShouldThrow<ArgumentException>().Where(x => x.ParamName == "exceptionHandlers");
        }

        [Fact]
        public void HandleEventException_invokes_all_handler_methods()
        {
            var fixture = new Fixture().Customize(new AutoMoqCustomization());
            var exceptionHandlers = fixture.Create<IEventMessageExceptionHandler[]>();
            var sut = new CompositeEventMessageExceptionHandler(exceptionHandlers);
            var context = fixture.Create<HandleEventExceptionContext>();

            sut.HandleEventException(context);

            foreach (IEventMessageExceptionHandler handler in exceptionHandlers)
            {
                Mock.Get(handler).Verify(x => x.HandleEventException(context), Times.Once());
            }
        }

        [Fact]
        public void HandleMessageException_invokes_all_handler_method()
        {
            var fixture = new Fixture().Customize(new AutoMoqCustomization());
            var exceptionHandlers = fixture.Create<IEventMessageExceptionHandler[]>();
            var sut = new CompositeEventMessageExceptionHandler(exceptionHandlers);
            var context = fixture.Create<HandleMessageExceptionContext>();

            sut.HandleMessageException(context);

            foreach (IEventMessageExceptionHandler handler in exceptionHandlers)
            {
                Mock.Get(handler).Verify(x => x.HandleMessageException(context), Times.Once());
            }
        }
    }
}
