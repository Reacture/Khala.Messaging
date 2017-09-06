﻿namespace Khala.Messaging
{
    using System.Threading;
    using System.Threading.Tasks;
    using FluentAssertions;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Moq;
    using Ploeh.AutoFixture;
    using Ploeh.AutoFixture.Idioms;

    [TestClass]
    public class DelegatingMessageHandler_specs
    {
        [TestMethod]
        public void sut_implements_IMessageHandler()
        {
            typeof(DelegatingMessageHandler).Should().Implement<IMessageHandler>();
        }

        [TestMethod]
        public void sut_has_guard_clauses()
        {
            var builder = new Fixture();
            var assertion = new GuardClauseAssertion(builder);
            assertion.Verify(typeof(DelegatingMessageHandler));
        }

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public async Task Handle_relays_to_handler_function_correctly(bool canceled)
        {
            var functionProvider = Mock.Of<IFunctionProvider>();
            var sut = new DelegatingMessageHandler(functionProvider.Func<Envelope, CancellationToken, Task>);
            var envelope = new Envelope(new object());
            var cancellationToken = new CancellationToken(canceled);

            await sut.Handle(envelope, cancellationToken);

            Mock.Get(functionProvider).Verify(x => x.Func<Envelope, CancellationToken, Task>(envelope, cancellationToken), Times.Once());
        }

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public async Task Handle_replays_to_modest_handler_function_correctly(bool canceled)
        {
            var functionProvider = Mock.Of<IFunctionProvider>();
            var sut = new DelegatingMessageHandler(functionProvider.Func<Envelope, Task>);
            var envelope = new Envelope(new object());
            var cancellationToken = new CancellationToken(canceled);

            await sut.Handle(envelope, cancellationToken);

            Mock.Get(functionProvider).Verify(x => x.Func<Envelope, Task>(envelope), Times.Once());
        }

        public interface IFunctionProvider
        {
            TResult Func<T, TResult>(T args);

            TResult Func<T1, T2, TResult>(T1 arg1, T2 arg2);
        }
    }
}