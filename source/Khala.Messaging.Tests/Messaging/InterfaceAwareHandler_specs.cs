namespace Khala.Messaging
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using AutoFixture;
    using AutoFixture.AutoMoq;
    using AutoFixture.Idioms;
    using FakeBlogEngine;
    using FluentAssertions;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Moq;

    [TestClass]
    public class InterfaceAwareHandler_specs
    {
        [TestMethod]
        public void class_is_abstract()
        {
            typeof(InterfaceAwareHandler).IsAbstract.Should().BeTrue();
        }

        [TestMethod]
        public void sut_implements_IMessageHandler()
        {
            InterfaceAwareHandler sut = Mock.Of<InterfaceAwareHandler>();
            sut.Should().BeAssignableTo<IMessageHandler>();
        }

        [TestMethod]
        public void sut_has_guard_clauses()
        {
            IFixture builder = new Fixture().Customize(new AutoMoqCustomization());
            var assertion = new GuardClauseAssertion(builder);
            assertion.Verify(typeof(InterfaceAwareHandler));
        }

        public abstract class BlogEventHandler :
            InterfaceAwareHandler,
            IHandles<BlogPostCreated>,
            IHandles<CommentedOnBlogPost>
        {
            public abstract Task Handle(
                Envelope<BlogPostCreated> envelope,
                CancellationToken cancellationToken);

            public abstract Task Handle(
                Envelope<CommentedOnBlogPost> envelope,
                CancellationToken cancellationToken);
        }

        public class UnknownMessage
        {
        }

        [TestMethod]
        public void Accepts_returns_true_if_sut_handles_message()
        {
            var message = new BlogPostCreated();
            var envelope = new Envelope(message);
            BlogEventHandler sut = Mock.Of<BlogEventHandler>();

            bool actual = sut.Accepts(envelope);

            actual.Should().BeTrue();
        }

        [TestMethod]
        public void Accepts_returns_false_if_sut_does_not_handle_message()
        {
            var message = new UnknownMessage();
            var envelope = new Envelope(message);
            BlogEventHandler sut = Mock.Of<BlogEventHandler>();

            bool actual = sut.Accepts(envelope);

            actual.Should().BeFalse();
        }

        [TestMethod]
        public async Task sut_invokes_correct_handler_method()
        {
            var mock = new Mock<BlogEventHandler> { CallBase = true };
            BlogEventHandler sut = mock.Object;
            var messageId = Guid.NewGuid();
            var operationId = Guid.NewGuid();
            var correlationId = Guid.NewGuid();
            string contributor = Guid.NewGuid().ToString();
            object message = new BlogPostCreated();
            var envelope = new Envelope(messageId, message, operationId, correlationId, contributor);

            await sut.Handle(envelope, CancellationToken.None);

            Mock.Get(sut).Verify(
                x =>
                x.Handle(
                    It.Is<Envelope<BlogPostCreated>>(
                        p =>
                        p.MessageId == messageId &&
                        p.OperationId == operationId &&
                        p.CorrelationId == correlationId &&
                        p.Contributor == contributor &&
                        p.Message == message),
                    CancellationToken.None),
                Times.Once());
        }
    }
}
