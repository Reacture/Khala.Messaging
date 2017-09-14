namespace Khala.Messaging
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Threading;
    using System.Threading.Tasks;
    using FluentAssertions;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Moq;
    using Ploeh.AutoFixture;
    using Ploeh.AutoFixture.AutoMoq;
    using Ploeh.AutoFixture.Idioms;

    [TestClass]
    public class ExceptionHandlingMessageBus_specs
    {
        [TestMethod]
        public void sut_implements_IMessageBus()
        {
            typeof(ExceptionHandlingMessageBus).Should().Implement<IMessageBus>();
        }

        [TestMethod]
        public void constructor_sets_ExceptionHandler_correctly()
        {
            var exceptionHandler = Mock.Of<IMessageBusExceptionHandler>();
            var sut = new ExceptionHandlingMessageBus(Mock.Of<IMessageBus>(), exceptionHandler);
            sut.ExceptionHandler.Should().BeSameAs(exceptionHandler);
        }

        [TestMethod]
        public void sut_has_ExceptionHandler_property()
        {
            typeof(ExceptionHandlingMessageBus).Should().HaveProperty<IMessageBusExceptionHandler>("ExceptionHandler");
        }

        [TestMethod]
        public void sut_is_immutable()
        {
            foreach (PropertyInfo property in typeof(ExceptionHandlingMessageBus).GetProperties())
            {
                property.Should().NotBeWritable();
            }
        }

        [TestMethod]
        public void sut_has_guard_clauses()
        {
            var builder = new Fixture().Customize(new AutoMoqCustomization());
            new GuardClauseAssertion(builder).Verify(typeof(ExceptionHandlingMessageBus));
        }

        [TestMethod]
        public void sut_has_MessageBus_property()
        {
            typeof(ExceptionHandlingMessageBus).Should().HaveProperty<IMessageBus>("MessageBus");
        }

        [TestMethod]
        public void constructor_sets_MessageBus_correctly()
        {
            var messageBus = Mock.Of<IMessageBus>();
            var sut = new ExceptionHandlingMessageBus(messageBus, Mock.Of<IMessageBusExceptionHandler>());
            sut.MessageBus.Should().BeSameAs(messageBus);
        }

        [TestMethod]
        public void given_message_bus_fails_Send_invokes_exception_handler()
        {
            // Arrange
            var envelope = new Envelope(new object());

            Exception exception = new InvalidOperationException();
            var messageBus = Mock.Of<IMessageBus>();
            Mock.Get(messageBus)
                .Setup(x => x.Send(envelope, CancellationToken.None))
                .Throws(exception);

            var exceptionHandler = Mock.Of<IMessageBusExceptionHandler>();

            var sut = new ExceptionHandlingMessageBus(messageBus, exceptionHandler);

            // Act
            Func<Task> action = () => sut.Send(envelope, CancellationToken.None);

            // Assert
            action.ShouldThrow<InvalidOperationException>().Which.Should().BeSameAs(exception);
            Mock.Get(exceptionHandler).Verify(
                x =>
                x.Handle(
                    It.Is<MessageBusExceptionContext>(
                        p =>
                        p.Envelopes.Count == 1 &&
                        p.Envelopes[0] == envelope &&
                        p.Exception == exception &&
                        p.Handled == false)),
                Times.Once());
        }

        [TestMethod]
        public void given_exception_handler_handles_exception_Send_does_not_throw_it()
        {
            // Arrange
            var envelope = new Envelope(new object());

            Exception exception = new InvalidOperationException();
            var messageBus = Mock.Of<IMessageBus>();
            Mock.Get(messageBus)
                .Setup(x => x.Send(envelope, CancellationToken.None))
                .Throws(exception);

            var exceptionHandler = new DelegatingMessageBusExceptionHandler(
                context =>
                {
                    if (context.Exception == exception)
                    {
                        context.Handled = true;
                    }

                    return Task.FromResult(true);
                });

            var sut = new ExceptionHandlingMessageBus(messageBus, exceptionHandler);

            // Act
            Func<Task> action = () => sut.Send(envelope, CancellationToken.None);

            // Assert
            action.ShouldNotThrow();
        }

        [TestMethod]
        public async Task SendBatch_relays_to_MessageBus()
        {
            // Arrange
            var messageBus = Mock.Of<IMessageBus>();
            IEnumerable<Envelope> envelopes = new[] { new Envelope(new object()) };

            var sut = new ExceptionHandlingMessageBus(
                messageBus,
                Mock.Of<IMessageBusExceptionHandler>());

            var cancellationToken = CancellationToken.None;

            // Act
            await sut.SendBatch(envelopes, cancellationToken);

            // Assert
            Mock.Get(messageBus).Verify(x => x.SendBatch(envelopes, cancellationToken), Times.Once());
        }

        [TestMethod]
        public void given_message_bus_fails_SendBatch_invokes_exception_handler()
        {
            // Arrange
            IEnumerable<Envelope> envelopes = new Fixture().CreateMany<Envelope>();

            Exception exception = new InvalidOperationException();
            var messageBus = Mock.Of<IMessageBus>();
            Mock.Get(messageBus)
                .Setup(x => x.SendBatch(envelopes, CancellationToken.None))
                .Throws(exception);

            var exceptionHandler = Mock.Of<IMessageBusExceptionHandler>();

            var sut = new ExceptionHandlingMessageBus(messageBus, exceptionHandler);

            // Act
            Func<Task> action = () => sut.SendBatch(envelopes, CancellationToken.None);

            // Assert
            action.ShouldThrow<InvalidOperationException>().Which.Should().BeSameAs(exception);
            Mock.Get(exceptionHandler).Verify(
                x =>
                x.Handle(
                    It.Is<MessageBusExceptionContext>(
                        p =>
                        p.Envelopes.Count == envelopes.Count() &&
                        p.Envelopes.SequenceEqual(envelopes) &&
                        p.Exception == exception &&
                        p.Handled == false)),
                Times.Once());
        }

        [TestMethod]
        public void given_exception_handler_handles_exception_SendBatch_does_not_throw_it()
        {
            // Arrange
            IEnumerable<Envelope> envelopes = new Fixture().CreateMany<Envelope>();

            Exception exception = new InvalidOperationException();
            var messageBus = Mock.Of<IMessageBus>();
            Mock.Get(messageBus)
                .Setup(x => x.SendBatch(envelopes, CancellationToken.None))
                .Throws(exception);

            var exceptionHandler = new DelegatingMessageBusExceptionHandler(
                context =>
                {
                    if (context.Exception == exception)
                    {
                        context.Handled = true;
                    }

                    return Task.FromResult(true);
                });

            var sut = new ExceptionHandlingMessageBus(messageBus, exceptionHandler);

            // Act
            Func<Task> action = () => sut.SendBatch(envelopes, CancellationToken.None);

            // Assert
            action.ShouldNotThrow();
        }
    }
}
