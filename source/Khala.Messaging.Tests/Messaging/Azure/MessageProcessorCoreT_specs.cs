namespace Khala.Messaging.Azure
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
    public class MessageProcessorCoreT_specs
    {
        [TestMethod]
        public void sut_has_guard_clauses()
        {
            var builder = new Fixture().Customize(new AutoMoqCustomization());
            var assertion = new GuardClauseAssertion(builder);
            assertion.Verify(typeof(MessageProcessorCore<>));
        }

        [TestMethod]
        public async Task Process_deserializes_message_once()
        {
            var envelope = new Envelope(new object());
            var source = new Data(envelope);
            var serializer = Mock.Of<IMessageDataSerializer<Data>>();
            var messageHandler = Mock.Of<IMessageHandler>();
            var exceptionHandler = Mock.Of<IMessageProcessingExceptionHandler<Data>>();
            var sut = new MessageProcessorCore<Data>(messageHandler, serializer, exceptionHandler);

            await sut.Process(source, Data.Checkpoint, CancellationToken.None);

            Mock.Get(serializer).Verify(x => x.Deserialize(source), Times.Once());
        }

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public async Task Process_invoke_message_handler_correctly(bool canceled)
        {
            var messageHandler = Mock.Of<IMessageHandler>();
            var envelope = new Envelope(new object());
            var cancellationToken = new CancellationToken(canceled);
            var sut = new MessageProcessorCore<Data>(
                messageHandler,
                DataSerializer.Instance,
                Mock.Of<IMessageProcessingExceptionHandler<Data>>());
            var source = new Data(envelope);

            await sut.Process(source, Data.Checkpoint, cancellationToken);

            Mock.Get(messageHandler).Verify(x => x.Handle(envelope, cancellationToken), Times.Once());
        }

        [TestMethod]
        public async Task Process_checkpoints()
        {
            var messageHandler = Mock.Of<IMessageHandler>();
            var sut = new MessageProcessorCore<Data>(
                messageHandler,
                DataSerializer.Instance,
                Mock.Of<IMessageProcessingExceptionHandler<Data>>());
            var source = new Data(new Envelope(new object()));
            var functionProvider = Mock.Of<IFunctionProvider>();

            await sut.Process(source, functionProvider.Func<Data, Task>, CancellationToken.None);

            Mock.Get(functionProvider).Verify(x => x.Func<Data, Task>(source), Times.Once());
        }

        [TestMethod]
        public void given_message_handler_fails_Process_invoke_exception_handler()
        {
            // Arrange
            var envelope = new Envelope(new object());
            var source = new Data(envelope);
            var exception = new InvalidOperationException();

            var messageHandler = Mock.Of<IMessageHandler>();
            Mock.Get(messageHandler)
                .Setup(x => x.Handle(envelope, CancellationToken.None))
                .Throws(exception);
            var exceptionHandler = Mock.Of<IMessageProcessingExceptionHandler<Data>>();

            var sut = new MessageProcessorCore<Data>(
                messageHandler,
                DataSerializer.Instance,
                exceptionHandler);

            // Act
            Func<Task> action = () => sut.Process(source, Data.Checkpoint, CancellationToken.None);

            // Assert
            action.ShouldThrow<InvalidOperationException>().Where(x => x == exception);
            Mock.Get(exceptionHandler).Verify(
                x =>
                x.Handle(
                    It.Is<MessageProcessingExceptionContext<Data>>(
                        p =>
                        p.Source == source &&
                        p.Envelope == envelope &&
                        p.Exception == exception &&
                        p.Handled == false)),
                Times.Once());
        }

        [TestMethod]
        public void given_exception_handler_handles_message_handler_error_Process_does_not_throw_exception()
        {
            // Arrange
            var envelope = new Envelope(new object());
            var exception = new InvalidOperationException();

            var messageHandler = Mock.Of<IMessageHandler>();
            Mock.Get(messageHandler)
                .Setup(x => x.Handle(envelope, CancellationToken.None))
                .Throws(exception);
            var exceptionHandler = Mock.Of<IMessageProcessingExceptionHandler<Data>>();
            Mock.Get(exceptionHandler)
                .Setup(x => x.Handle(It.Is<MessageProcessingExceptionContext<Data>>(p => p.Exception == exception)))
                .Callback<MessageProcessingExceptionContext<Data>>(context => context.Handled = true)
                .Returns(Task.FromResult(true));

            var sut = new MessageProcessorCore<Data>(
                messageHandler,
                DataSerializer.Instance,
                exceptionHandler);

            // Act
            Func<Task> action = () =>
            sut.Process(new Data(envelope), Data.Checkpoint, CancellationToken.None);

            // Assert
            action.ShouldNotThrow();
        }

        [TestMethod]
        public void given_message_handler_fails_Process_consumes_exception_handler_exception()
        {
            // Arrange
            var envelope = new Envelope(new object());

            var messageHandler = Mock.Of<IMessageHandler>();
            var messageHandlerException = new Exception();
            Mock.Get(messageHandler)
                .Setup(x => x.Handle(envelope, CancellationToken.None))
                .Throws(messageHandlerException);

            var exceptionHandler = Mock.Of<IMessageProcessingExceptionHandler<Data>>();
            var exceptionHandlerException = new InvalidOperationException();
            Mock.Get(exceptionHandler)
                .Setup(x => x.Handle(It.IsAny<MessageProcessingExceptionContext<Data>>()))
                .Throws(exceptionHandlerException);

            var sut = new MessageProcessorCore<Data>(
                messageHandler,
                DataSerializer.Instance,
                exceptionHandler);

            // Act
            Func<Task> action = () =>
            sut.Process(new Data(envelope), Data.Checkpoint, CancellationToken.None);

            // Assert
            action.ShouldNotThrow<InvalidOperationException>();
            action.ShouldThrowExactly<Exception>().Where(x => x == messageHandlerException);
        }

        [TestMethod]
        public void given_deserializer_fails_Process_invokes_exception_handler()
        {
            var source = new Data(new Envelope(new object()));
            var messageHandler = Mock.Of<IMessageHandler>();
            var serializer = Mock.Of<IMessageDataSerializer<Data>>();
            var exception = new Exception();
            var exceptionHandler = Mock.Of<IMessageProcessingExceptionHandler<Data>>();
            Mock.Get(serializer).Setup(x => x.Deserialize(source)).Throws(exception);
            var sut = new MessageProcessorCore<Data>(
                messageHandler,
                serializer,
                exceptionHandler);

            Func<Task> action = () =>
            sut.Process(source, Data.Checkpoint, CancellationToken.None);

            action.ShouldThrow<Exception>().Where(x => x == exception);
            Mock.Get(exceptionHandler).Verify(
                x =>
                x.Handle(
                    It.Is<MessageProcessingExceptionContext<Data>>(
                        p =>
                        p.Source == source &&
                        p.Envelope == null &&
                        p.Exception == exception &&
                        p.Handled == false)),
                Times.Once());
        }

        [TestMethod]
        public void given_checkpointer_fails_Process_invokes_exception_handler()
        {
            var envelope = new Envelope(new object());
            var source = new Data(envelope);
            var exception = new Exception();
            var exceptionHandler = Mock.Of<IMessageProcessingExceptionHandler<Data>>();
            var sut = new MessageProcessorCore<Data>(
                Mock.Of<IMessageHandler>(),
                DataSerializer.Instance,
                exceptionHandler);

            Func<Task> action = () =>
            sut.Process(source, x => throw exception, CancellationToken.None);

            action.ShouldThrow<Exception>().Where(x => x == exception);
            Mock.Get(exceptionHandler).Verify(
                x =>
                x.Handle(
                    It.Is<MessageProcessingExceptionContext<Data>>(
                        p =>
                        p.Source == source &&
                        p.Envelope == envelope &&
                        p.Exception == exception &&
                        p.Handled == false)),
                Times.Once());
        }

        public interface IFunctionProvider
        {
            TResult Func<T, TResult>(T arg);
        }

        public class Data
        {
            public Data(Envelope envelope)
            {
                Envelope = envelope;
            }

            public Envelope Envelope { get; }

            public static Task<Envelope> Deserialize(Data source) => Task.FromResult(source.Envelope);

            public static Task Checkpoint(Data source) => Task.FromResult(true);
        }

        public class DataSerializer : IMessageDataSerializer<Data>
        {
            public static readonly DataSerializer Instance = new DataSerializer();

            public Task<Envelope> Deserialize(Data data) => Task.FromResult(data.Envelope);

            public Task<Data> Serialize(Envelope envelope) => Task.FromResult(new Data(envelope));
        }
    }
}
