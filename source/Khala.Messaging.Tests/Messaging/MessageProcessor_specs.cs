namespace Khala.Messaging
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using FluentAssertions;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Moq;
    using Ploeh.AutoFixture;
    using Ploeh.AutoFixture.AutoMoq;
    using Ploeh.AutoFixture.Idioms;

    [TestClass]
    public class MessageProcessor_specs
    {
        [TestMethod]
        public void sut_has_guard_clauses()
        {
            var builder = new Fixture().Customize(new AutoMoqCustomization());
            new GuardClauseAssertion(builder).Verify(typeof(MessageProcessor<>));
        }

        [TestMethod]
        public async Task Process_deserializes_message_once()
        {
            var data = new Data(new Envelope(new object()));
            var serializer = Mock.Of<IMessageDataSerializer<Data>>();
            var sut = new MessageProcessor<Data>(
                serializer,
                Mock.Of<IMessageHandler>(),
                Mock.Of<IMessageProcessingExceptionHandler<Data>>());
            var context = new MessageContext<Data>(data, d => Task.CompletedTask);

            await sut.Process(context, CancellationToken.None);

            Mock.Get(serializer).Verify(x => x.Deserialize(data), Times.Once());
        }

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public async Task Process_invoke_message_handler_correctly(bool canceled)
        {
            var messageHandler = Mock.Of<IMessageHandler>();
            var envelope = new Envelope(new object());
            var cancellationToken = new CancellationToken(canceled);
            var sut = new MessageProcessor<Data>(
                DataSerializer.Instance,
                messageHandler,
                Mock.Of<IMessageProcessingExceptionHandler<Data>>());
            var context = new MessageContext<Data>(new Data(envelope), d => Task.CompletedTask);

            await sut.Process(context, cancellationToken);

            Mock.Get(messageHandler).Verify(x => x.Handle(envelope, cancellationToken), Times.Once());
        }

        [TestMethod]
        public async Task Process_invokes_Acknowledge()
        {
            var sut = new MessageProcessor<Data>(
                DataSerializer.Instance,
                Mock.Of<IMessageHandler>(),
                Mock.Of<IMessageProcessingExceptionHandler<Data>>());
            var data = new Data(new Envelope(new object()));
            var functionProvider = Mock.Of<IFunctionProvider>();
            var context = new MessageContext<Data>(data, functionProvider.Func);

            await sut.Process(context, CancellationToken.None);

            Mock.Get(functionProvider).Verify(x => x.Func(data), Times.Once());
        }

        [TestMethod]
        public void given_message_handler_fails_Process_invoke_exception_handler()
        {
            // Arrange
            var envelope = new Envelope(new object());
            var data = new Data(envelope);
            var exception = new InvalidOperationException();

            var messageHandler = Mock.Of<IMessageHandler>();
            Mock.Get(messageHandler)
                .Setup(x => x.Handle(envelope, CancellationToken.None))
                .Throws(exception);
            var exceptionHandler = Mock.Of<IMessageProcessingExceptionHandler<Data>>();

            var sut = new MessageProcessor<Data>(
                DataSerializer.Instance,
                messageHandler,
                exceptionHandler);

            var context = new MessageContext<Data>(data, d => Task.CompletedTask);

            // Act
            Func<Task> action = () => sut.Process(context, CancellationToken.None);

            // Assert
            action.ShouldThrow<InvalidOperationException>().Where(x => x == exception);
            Mock.Get(exceptionHandler).Verify(
                x =>
                x.Handle(
                    It.Is<MessageProcessingExceptionContext<Data>>(
                        p =>
                        p.Data == data &&
                        p.Envelope == envelope &&
                        p.Exception == exception &&
                        p.Handled == false)),
                Times.Once());
        }

        [TestMethod]
        public void given_deserializer_fails_Process_invokes_exception_handler()
        {
            // Arrange
            var data = new Data(new Envelope(new object()));

            var serializer = Mock.Of<IMessageDataSerializer<Data>>();
            var exception = new Exception();
            Mock.Get(serializer).Setup(x => x.Deserialize(data)).Throws(exception);

            var exceptionHandler = Mock.Of<IMessageProcessingExceptionHandler<Data>>();

            var sut = new MessageProcessor<Data>(
                serializer,
                Mock.Of<IMessageHandler>(),
                exceptionHandler);

            var context = new MessageContext<Data>(data, d => Task.CompletedTask);

            // Act
            Func<Task> action = () => sut.Process(context, CancellationToken.None);

            // Assert
            action.ShouldThrow<Exception>().Where(x => x == exception);
            Mock.Get(exceptionHandler).Verify(
                x =>
                x.Handle(
                    It.Is<MessageProcessingExceptionContext<Data>>(
                        p =>
                        p.Data == data &&
                        p.Envelope == null &&
                        p.Exception == exception &&
                        p.Handled == false)),
                Times.Once());
        }

        [TestMethod]
        public void given_Acknowledge_fails_Process_invokes_exception_handler()
        {
            // Arrange
            var envelope = new Envelope(new object());

            var exceptionHandler = Mock.Of<IMessageProcessingExceptionHandler<Data>>();
            var sut = new MessageProcessor<Data>(
                DataSerializer.Instance,
                Mock.Of<IMessageHandler>(),
                exceptionHandler);

            var data = new Data(envelope);
            var exception = new Exception();
            var context = new MessageContext<Data>(data, d => throw exception);

            // Act
            Func<Task> action = () => sut.Process(context, CancellationToken.None);

            // Assert
            action.ShouldThrow<Exception>().Where(x => x == exception);
            Mock.Get(exceptionHandler).Verify(
                x =>
                x.Handle(
                    It.Is<MessageProcessingExceptionContext<Data>>(
                        p =>
                        p.Data == data &&
                        p.Envelope == envelope &&
                        p.Exception == exception &&
                        p.Handled == false)),
                Times.Once());
        }

        [TestMethod]
        public void given_message_handler_fails_Process_does_not_invoke_Acknowledge()
        {
            // Arrange
            var envelope = new Envelope(new object());

            var messageHandler = Mock.Of<IMessageHandler>();
            Mock.Get(messageHandler)
                .Setup(x => x.Handle(envelope, CancellationToken.None))
                .Throws(new Exception());

            var sut = new MessageProcessor<Data>(
                DataSerializer.Instance,
                messageHandler,
                Mock.Of<IMessageProcessingExceptionHandler<Data>>());

            var data = new Data(envelope);
            var functionProvider = Mock.Of<IFunctionProvider>();
            var context = new MessageContext<Data>(data, functionProvider.Func);

            // Act
            Func<Task> action = () => sut.Process(context, CancellationToken.None);

            // Assert
            action.ShouldThrow<Exception>();
            Mock.Get(functionProvider).Verify(x => x.Func(data), Times.Never());
        }

        [TestMethod]
        public void given_exception_handler_handles_exception_Process_does_not_throw()
        {
            // Arrange
            var envelope = new Envelope(new object());

            var messageHandler = Mock.Of<IMessageHandler>();
            Mock.Get(messageHandler)
                .Setup(x => x.Handle(envelope, CancellationToken.None))
                .Throws(new InvalidOperationException());

            var sut = new MessageProcessor<Data>(
                DataSerializer.Instance,
                messageHandler,
                new DelegatingMessageProcessingExceptionHandler<Data>(x =>
                {
                    x.Handled = true;
                    return Task.CompletedTask;
                }));

            var context = new MessageContext<Data>(new Data(envelope), d => Task.CompletedTask);

            // Act
            Func<Task> action = () => sut.Process(context, CancellationToken.None);

            // Assert
            action.ShouldNotThrow();
        }

        [TestMethod]
        public void Process_absorbs_exception_handler_exception()
        {
            // Arrange
            var envelope = new Envelope(new object());

            var messageHandler = Mock.Of<IMessageHandler>();
            var exception = new Exception();
            Mock.Get(messageHandler)
                .Setup(x => x.Handle(envelope, CancellationToken.None))
                .Throws(exception);

            var sut = new MessageProcessor<Data>(
                DataSerializer.Instance,
                messageHandler,
                new DelegatingMessageProcessingExceptionHandler<Data>(
                    x => throw new InvalidOperationException()));

            var context = new MessageContext<Data>(new Data(envelope), d => Task.CompletedTask);

            // Act
            Func<Task> action = () => sut.Process(context, CancellationToken.None);

            // Assert
            action.ShouldNotThrow<InvalidOperationException>();
            action.ShouldThrowExactly<Exception>().Where(x => x == exception);
        }

        public interface IFunctionProvider
        {
            Task Func<T>(T arg);
        }

        public class Data
        {
            public Data(Envelope envelope) => Envelope = envelope;

            public Envelope Envelope { get; }
        }

        public class DataSerializer : IMessageDataSerializer<Data>
        {
            public static readonly DataSerializer Instance = new DataSerializer();

            public Task<Envelope> Deserialize(Data data) => Task.FromResult(data.Envelope);

            public Task<Data> Serialize(Envelope envelope) => throw new NotSupportedException();
        }
    }
}
