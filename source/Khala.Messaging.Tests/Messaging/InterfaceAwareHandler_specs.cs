namespace Khala.Messaging
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using FakeBlogEngine;
    using FluentAssertions;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Moq;
    using Ploeh.AutoFixture;
    using Ploeh.AutoFixture.AutoMoq;
    using Ploeh.AutoFixture.Idioms;

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
            var sut = Mock.Of<InterfaceAwareHandler>();
            sut.Should().BeAssignableTo<IMessageHandler>();
        }

        [TestMethod]
        public void sut_has_guard_clauses()
        {
            var builder = new Fixture().Customize(new AutoMoqCustomization());
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

        [TestMethod]
        public async Task sut_invokes_correct_handler_method()
        {
            var mock = new Mock<BlogEventHandler> { CallBase = true };
            BlogEventHandler sut = mock.Object;
            Guid messageId = Guid.NewGuid();
            Guid correlationId = Guid.NewGuid();
            string contributor = Guid.NewGuid().ToString();
            object message = new BlogPostCreated();
            var envelope = new Envelope(messageId, correlationId, contributor, message);

            await sut.Handle(envelope, CancellationToken.None);

            Mock.Get(sut).Verify(
                x =>
                x.Handle(
                    It.Is<Envelope<BlogPostCreated>>(
                        p =>
                        p.MessageId == messageId &&
                        p.CorrelationId == correlationId &&
                        p.Contributor == contributor &&
                        p.Message == message),
                    CancellationToken.None),
                Times.Once());
        }
    }
}
