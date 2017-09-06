using System;
using System.Threading;
using System.Threading.Tasks;
using FakeBlogEngine;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Khala.Messaging
{
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
            var sut = Mock.Of<Messaging.InterfaceAwareHandler>();
            sut.Should().BeAssignableTo<IMessageHandler>();
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
            object message = new BlogPostCreated();
            Guid correlationId = Guid.NewGuid();
            var envelope = new Envelope(correlationId, message);

            await sut.Handle(envelope, CancellationToken.None);

            Mock.Get(sut).Verify(
                x =>
                x.Handle(
                    It.Is<Envelope<BlogPostCreated>>(
                        p =>
                        p.MessageId == envelope.MessageId &&
                        p.CorrelationId == correlationId &&
                        p.Message == message),
                    CancellationToken.None),
                Times.Once());
        }
    }
}
