namespace Khala.Messaging
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Threading;
    using System.Threading.Tasks;
    using FluentAssertions;
    using Khala.TransientFaultHandling;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Moq;
    using Ploeh.AutoFixture;
    using Ploeh.AutoFixture.AutoMoq;
    using Ploeh.AutoFixture.Idioms;

    [TestClass]
    public class TransientFaultHandlingMessageBus_specs
    {
        [TestMethod]
        public void sut_implements_IMessageBus()
        {
            typeof(TransientFaultHandlingMessageBus).Should().Implement<IMessageBus>();
        }

        [TestMethod]
        public void constructor_sets_RetryPolicy_correctly()
        {
            IFixture fixture = new Fixture().Customize(new AutoMoqCustomization());
            RetryPolicy retryPolicy = fixture.Create<RetryPolicy>();
            IMessageBus messageBus = Mock.Of<IMessageBus>();

            var sut = new TransientFaultHandlingMessageBus(retryPolicy, messageBus);

            sut.RetryPolicy.Should().BeSameAs(retryPolicy);
        }

        [TestMethod]
        public void sut_is_immutable()
        {
            foreach (PropertyInfo property in typeof(TransientFaultHandlingMessageBus).GetProperties())
            {
                property.Should().NotBeWritable();
            }
        }

        [TestMethod]
        public void sut_has_RetryPolicy_property()
        {
            typeof(TransientFaultHandlingMessageBus).Should().HaveProperty<IRetryPolicy>("RetryPolicy");
        }

        [TestMethod]
        public void sut_has_MessageBus_property()
        {
            typeof(TransientFaultHandlingMessageBus).Should().HaveProperty<IMessageBus>("MessageBus");
        }

        [TestMethod]
        public void constructor_sets_MessageBus_correctly()
        {
            IFixture fixture = new Fixture().Customize(new AutoMoqCustomization());
            RetryPolicy retryPolicy = fixture.Create<RetryPolicy>();
            IMessageBus messageBus = Mock.Of<IMessageBus>();

            var sut = new TransientFaultHandlingMessageBus(retryPolicy, messageBus);

            sut.MessageBus.Should().BeSameAs(messageBus);
        }

        [TestMethod]
        public void sut_has_guard_clauses()
        {
            IFixture builder = new Fixture().Customize(new AutoMoqCustomization());
            new GuardClauseAssertion(builder).Verify(typeof(TransientFaultHandlingMessageBus));
        }

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public async Task Send_relays_to_retry_policy(bool canceled)
        {
            IFixture fixture = new Fixture().Customize(new AutoMoqCustomization());
            IRetryPolicy retryPolicy = Mock.Of<IRetryPolicy>();
            IMessageBus messageBus = Mock.Of<IMessageBus>();
            var sut = new TransientFaultHandlingMessageBus(retryPolicy, messageBus);
            var envelope = new Envelope(new object());
            var cancellationToken = new CancellationToken(canceled);

            await sut.Send(envelope, cancellationToken);

            Func<Envelope, CancellationToken, Task> func = messageBus.Send;
            Mock.Get(retryPolicy).Verify(
                x =>
                x.Run(
                    It.Is<Func<Envelope, CancellationToken, Task>>(
                        p =>
                        p.Method == func.Method &&
                        p.Target == func.Target),
                    envelope,
                    cancellationToken),
                Times.Once());
        }

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public async Task Send_with_envelopes_relays_to_retry_policy(bool canceled)
        {
            IFixture fixture = new Fixture().Customize(new AutoMoqCustomization());
            IRetryPolicy retryPolicy = Mock.Of<IRetryPolicy>();
            IMessageBus messageBus = Mock.Of<IMessageBus>();
            var sut = new TransientFaultHandlingMessageBus(retryPolicy, messageBus);
            Envelope[] envelopes = new[] { new Envelope(new object()) };
            var cancellationToken = new CancellationToken(canceled);

            await sut.Send(envelopes, cancellationToken);

            Func<IEnumerable<Envelope>, CancellationToken, Task> func = messageBus.Send;
            Mock.Get(retryPolicy).Verify(
                x =>
                x.Run(
                    It.Is<Func<IEnumerable<Envelope>, CancellationToken, Task>>(
                        p =>
                        p.Method == func.Method &&
                        p.Target == func.Target),
                    envelopes,
                    cancellationToken),
                Times.Once());
        }
    }
}
